using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

// ============================================================
//  BAZAR — Classroom Card Game  (Console Edition)
// ============================================================

namespace Bazar
{
    // ─── Enums ───────────────────────────────────────────────

    enum CardType
    {
        // Shopkeeper cards
        BluffPrice, FlashTax, BiddingWar, PriceHike,
        LoyaltyPenalty, AuctionFreeze, NoDiscountZone,
        StealBack, DoubleOrNothing,
        // Customer cards
        SalaryDay, Pickpocket, Negotiator, Heist,
        ItemSnatch, MarketResearch, PriceDrop,
        Sabotage, GroupPressure
    }

    enum CardOwner { Shopkeeper, Customer }

    // ─── Card Definition ─────────────────────────────────────

    class Card
    {
        public CardType   Type        { get; }
        public string     Name        { get; }
        public string     ShortEffect { get; }
        public string     Tag         { get; }
        public CardOwner  Owner       { get; }
        public int        RollMin     { get; }
        public int        RollMax     { get; }

        public Card(CardType t, string name, string tag, string effect,
                    CardOwner owner, int rollMin, int rollMax)
        {
            Type = t; Name = name; Tag = tag;
            ShortEffect = effect; Owner = owner;
            RollMin = rollMin; RollMax = rollMax;
        }
    }

    // ─── Player ──────────────────────────────────────────────

    class Player
    {
        public string Name        { get; }
        public bool   IsHuman     { get; }
        public bool   IsShopkeeper{ get; }
        public int    Money       { get; set; }
        public int    ItemsWon    { get; set; }
        public bool   IsEliminated{ get; set; }

        // Per-round state
        public Card   DrawnCard1  { get; set; }
        public Card   DrawnCard2  { get; set; }
        public Card   PlayedCard  { get; set; }
        public int    Bid         { get; set; }
        public bool   PassedBid   { get; set; }

        // Carry-over effects
        public bool   NextRoundSeeRealPrice { get; set; }
        public int    NextRoundBonusMoney   { get; set; }
        public bool   SkipThisRound        { get; set; }

        public Player(string name, bool isHuman, bool isShopkeeper, int startMoney)
        {
            Name = name; IsHuman = isHuman;
            IsShopkeeper = isShopkeeper;
            Money = startMoney;
        }
    }

    // ─── Round Result ────────────────────────────────────────

    class RoundResult
    {
        public Player   Winner          { get; set; }   // null = unsold
        public int      WinningBid      { get; set; }
        public int      ActualPrice     { get; set; }
        public string   ItemName        { get; set; }
        public bool     ItemSnatched    { get; set; }
        public Player   Snatcher        { get; set; }
        public List<string> EventLog    { get; } = new List<string>();
    }

    // ─── Main Game ───────────────────────────────────────────

    class Game
    {
        static readonly Random rng = new Random();

        // Full card pool
        static readonly List<Card> ShopkeeperDeck = new List<Card>
        {
            new Card(CardType.BluffPrice,      "Bluff Price",       "BLUFF",   "Show ±50 fake price. Winner pays real price.",           CardOwner.Shopkeeper, 1,  2),
            new Card(CardType.FlashTax,        "Flash Tax",         "PENALTY", "Pick 1–2 customers. They each pay +Rs.50 to you.",       CardOwner.Shopkeeper, 3,  3),
            new Card(CardType.BiddingWar,      "Bidding War",       "FORCED",  "All must bid ≥Rs.50 or pay Rs.30 penalty.",              CardOwner.Shopkeeper, 4,  4),
            new Card(CardType.PriceHike,       "Price Hike",        "NEXT RND","Announce: next item +Rs.50. Panics bidders now.",        CardOwner.Shopkeeper, 5,  5),
            new Card(CardType.LoyaltyPenalty,  "Loyalty Penalty",   "PENALTY", "Customer with most items pays Rs.80 to you now.",        CardOwner.Shopkeeper, 6,  6),
            new Card(CardType.AuctionFreeze,   "Auction Freeze",    "CANCEL",  "Pick 1 customer. Their card does nothing.",              CardOwner.Shopkeeper, 7,  7),
            new Card(CardType.NoDiscountZone,  "No Discount Zone",  "NEXT RND","Next round: all price-cut customer cards blocked.",      CardOwner.Shopkeeper, 8,  8),
            new Card(CardType.StealBack,       "Steal Back",        "COUNTER", "Any steal card used this round costs them Rs.60.",       CardOwner.Shopkeeper, 9,  9),
            new Card(CardType.DoubleOrNothing, "Double or Nothing", "GAMBLE",  "Winner pays 2× bid. Roll 6–10: item worth 2 pts.",       CardOwner.Shopkeeper, 10, 10),
        };

        static readonly List<Card> CustomerDeck = new List<Card>
        {
            new Card(CardType.SalaryDay,     "Salary Day",      "GAIN",     "Gain Rs.100 to your budget now.",                        CardOwner.Customer, 1,  2),
            new Card(CardType.Pickpocket,    "Pickpocket",      "STEAL",    "Anyone who bids >Rs.200 loses Rs.50 to you.",            CardOwner.Customer, 3,  3),
            new Card(CardType.Negotiator,    "Negotiator",      "DISCOUNT", "If you win, pay Rs.70 less.",                            CardOwner.Customer, 4,  4),
            new Card(CardType.Heist,         "Heist",           "STEAL",    "Target 1 player. Roll 3–10: steal Rs.80. 1–2: pay Rs.40.",CardOwner.Customer, 5,  5),
            new Card(CardType.ItemSnatch,    "Item Snatch",     "STEAL",    "After winner decided, roll 4–10: steal item at base price.",CardOwner.Customer, 6, 6),
            new Card(CardType.MarketResearch,"Market Research", "NEXT RND", "Skip bidding. Next round: +Rs.60 + see real price.",     CardOwner.Customer, 7,  7),
            new Card(CardType.PriceDrop,     "Price Drop",      "DISCOUNT", "Item price drops Rs.60 for everyone.",                   CardOwner.Customer, 8,  8),
            new Card(CardType.Sabotage,      "Sabotage",        "CANCEL",   "Pick 1 customer. Their card does nothing this round.",   CardOwner.Customer, 9,  9),
            new Card(CardType.GroupPressure, "Group Pressure",  "VOTE",     "Unanimous: item -Rs.100. Defector gets +Rs.50, others -Rs.30.", CardOwner.Customer, 10, 10),
        };

        static readonly string[] ItemNames = {
            "Vintage Rug", "Silk Scarf", "Brass Lamp", "Spice Box", "Ceramic Vase"
        };

        // ── Console helpers ──────────────────────────────────

        static void SetColor(ConsoleColor fg, ConsoleColor bg = ConsoleColor.Black)
        {
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }

        static void ResetColor() => Console.ResetColor();

        static void Print(string text, ConsoleColor color = ConsoleColor.White)
        {
            SetColor(color);
            Console.Write(text);
            ResetColor();
        }

        static void PrintLine(string text = "", ConsoleColor color = ConsoleColor.White)
        {
            SetColor(color);
            Console.WriteLine(text);
            ResetColor();
        }

        static void Divider(char c = '─', int width = 60, ConsoleColor color = ConsoleColor.DarkGray)
        {
            PrintLine(new string(c, width), color);
        }

        static void Header(string title, ConsoleColor color = ConsoleColor.Yellow)
        {
            Console.WriteLine();
            Divider('═', 60, color);
            int pad = (60 - title.Length) / 2;
            PrintLine(new string(' ', Math.Max(0, pad)) + title, color);
            Divider('═', 60, color);
            Console.WriteLine();
        }

        static void BoxLine(string text, ConsoleColor color = ConsoleColor.Cyan)
        {
            Print("  │ ", ConsoleColor.DarkGray);
            PrintLine(text.PadRight(54), color);
            Print("  │ ", ConsoleColor.DarkGray);
            PrintLine(new string(' ', 54), ConsoleColor.DarkGray);
        }

        static void Pause(string prompt = "  Press ENTER to continue...")
        {
            Console.WriteLine();
            Print(prompt, ConsoleColor.DarkGray);
            Console.ReadLine();
            Console.WriteLine();
        }

        static int RollDie(string label = null)
        {
            int roll = rng.Next(1, 11);
            if (label != null)
            {
                Print($"  [ROLL] ", ConsoleColor.DarkYellow);
                Print(label + ": ", ConsoleColor.Gray);
                PrintLine($"{roll}", ConsoleColor.Yellow);
            }
            return roll;
        }

        static Card DrawCard(bool isShopkeeper)
        {
            int roll = rng.Next(1, 11);
            var deck = isShopkeeper ? ShopkeeperDeck : CustomerDeck;
            return deck.FirstOrDefault(c => roll >= c.RollMin && roll <= c.RollMax)
                   ?? deck[rng.Next(deck.Count)];
        }

        static void DisplayCard(Card card, bool isRevealed = true)
        {
            if (!isRevealed)
            {
                Print("  ┌────────────────────┐\n", ConsoleColor.DarkGray);
                Print("  │  ", ConsoleColor.DarkGray);
                Print("  [FACE DOWN]       ", ConsoleColor.DarkGray);
                PrintLine("  │", ConsoleColor.DarkGray);
                Print("  └────────────────────┘\n", ConsoleColor.DarkGray);
                return;
            }
            ConsoleColor tagColor = card.Owner == CardOwner.Shopkeeper
                ? ConsoleColor.DarkYellow : ConsoleColor.DarkCyan;
            ConsoleColor nameColor = card.Owner == CardOwner.Shopkeeper
                ? ConsoleColor.Yellow : ConsoleColor.Cyan;

            Print("  ┌────────────────────────────────┐\n", ConsoleColor.DarkGray);
            Print("  │ ", ConsoleColor.DarkGray);
            Print($" {card.Name,-20}", nameColor);
            Print($" [{card.Tag,-8}]", tagColor);
            Print(" │\n", ConsoleColor.DarkGray);
            Print("  │ ", ConsoleColor.DarkGray);
            Print($" {card.ShortEffect,-30}", ConsoleColor.Gray);
            Print("  │\n", ConsoleColor.DarkGray);
            Print("  └────────────────────────────────┘\n", ConsoleColor.DarkGray);
        }

        static void ShowMoney(List<Player> players)
        {
            Console.WriteLine();
            Divider('─', 60, ConsoleColor.DarkGray);
            PrintLine("  CURRENT STANDINGS", ConsoleColor.DarkGray);
            Divider('─', 60, ConsoleColor.DarkGray);
            foreach (var p in players)
            {
                string role = p.IsShopkeeper ? "Shopkeeper" : $"Items: {p.ItemsWon}";
                ConsoleColor c = p.IsEliminated ? ConsoleColor.DarkRed
                               : p.IsShopkeeper ? ConsoleColor.Yellow
                               : ConsoleColor.Cyan;
                string status = p.IsEliminated ? " [ELIMINATED]" : "";
                Print($"  {p.Name,-18}", c);
                Print($"Rs. {p.Money,5}", ConsoleColor.White);
                Print($"   {role}", ConsoleColor.DarkGray);
                PrintLine(status, ConsoleColor.DarkRed);
            }
            Divider('─', 60, ConsoleColor.DarkGray);
            Console.WriteLine();
        }

        // ── Rules Display ────────────────────────────────────

        static void ShowRules()
        {
            Console.Clear();
            Header("BAZAR — RULES", ConsoleColor.Yellow);

            PrintLine("  SETUP", ConsoleColor.Yellow);
            PrintLine("  • 1 Shopkeeper  vs  3 Customers", ConsoleColor.Gray);
            PrintLine("  • Shopkeeper starts at Rs. 0", ConsoleColor.Gray);
            PrintLine("  • Each Customer starts at Rs. 400", ConsoleColor.Gray);
            PrintLine("  • 5 rounds, 1 item per round", ConsoleColor.Gray);
            Console.WriteLine();

            PrintLine("  EACH ROUND", ConsoleColor.Yellow);
            PrintLine("  1. Shopkeeper sets item price (Rs. 100–200)", ConsoleColor.Gray);
            PrintLine("  2. Everyone rolls twice, picks 1 card to play", ConsoleColor.Gray);
            PrintLine("  3. All cards flip simultaneously — resolved in order", ConsoleColor.Gray);
            PrintLine("  4. Customers bid openly. Highest bid wins the item", ConsoleColor.Gray);
            PrintLine("  5. Winner pays. Money tracker updated.", ConsoleColor.Gray);
            Console.WriteLine();

            PrintLine("  WIN CONDITIONS", ConsoleColor.Yellow);
            Print("  Shopkeeper wins", ConsoleColor.Green);
            PrintLine(" → earn Rs. 800+ total", ConsoleColor.Gray);
            Print("  Shopkeeper loses", ConsoleColor.Red);
            PrintLine(" → earn under Rs. 500 or 3+ items unsold", ConsoleColor.Gray);
            Print("  Customer wins", ConsoleColor.Green);
            PrintLine(" → most items won. Tiebreak: most money left", ConsoleColor.Gray);
            Print("  Customer eliminated", ConsoleColor.Red);
            PrintLine(" → drops below Rs. 50", ConsoleColor.Gray);
            Console.WriteLine();

            PrintLine("  KEY RULES", ConsoleColor.Yellow);
            PrintLine("  • Bluff Price: displayed price ≠ real price. Pay real after reveal.", ConsoleColor.Gray);
            PrintLine("  • Chance cards (Heist, Item Snatch): roll live to resolve.", ConsoleColor.Gray);
            PrintLine("  • Next Round cards: effect carries to the next round only.", ConsoleColor.Gray);
            PrintLine("  • Can't afford bid? Forfeit. Second-highest wins instead.", ConsoleColor.Gray);
            PrintLine("  • Unsold round: no bids or all pass. Shopkeeper earns nothing.", ConsoleColor.Gray);
            Console.WriteLine();

            Divider('─', 60, ConsoleColor.DarkGray);
            PrintLine("  SHOPKEEPER CARDS  (roll 1–10 to draw)", ConsoleColor.DarkYellow);
            Divider('─', 60, ConsoleColor.DarkGray);
            foreach (var c in ShopkeeperDeck)
                PrintLine($"  {c.RollMin,2}–{c.RollMax,-2}  {c.Name,-22}  {c.ShortEffect}", ConsoleColor.DarkYellow);

            Console.WriteLine();
            Divider('─', 60, ConsoleColor.DarkGray);
            PrintLine("  CUSTOMER CARDS  (roll 1–10 to draw)", ConsoleColor.DarkCyan);
            Divider('─', 60, ConsoleColor.DarkGray);
            foreach (var c in CustomerDeck)
                PrintLine($"  {c.RollMin,2}–{c.RollMax,-2}  {c.Name,-22}  {c.ShortEffect}", ConsoleColor.DarkCyan);

            Pause("\n  Press ENTER to start the game...");
        }

        // ── AI decision helpers ──────────────────────────────

        static Card AiPickCard(Player ai, Card c1, Card c2, int itemPrice,
                               List<Player> customers, Player shopkeeper)
        {
            // Simple heuristic: prefer money-gaining or disruptive cards
            int Score(Card c)
            {
                return c.Type switch {
                    CardType.SalaryDay      => 3,
                    CardType.Negotiator     => ai.Money < 200 ? 4 : 2,
                    CardType.PriceDrop      => 3,
                    CardType.Heist          => 2,
                    CardType.Sabotage       => 2,
                    CardType.ItemSnatch     => customers.Count(p => p.ItemsWon > 0) > 0 ? 3 : 1,
                    CardType.MarketResearch => ai.Money < 150 ? 3 : 1,
                    CardType.Pickpocket     => 2,
                    CardType.GroupPressure  => 1,
                    // SK cards
                    CardType.BluffPrice     => 3,
                    CardType.FlashTax       => 2,
                    CardType.BiddingWar     => 2,
                    CardType.StealBack      => 2,
                    CardType.DoubleOrNothing=> 2,
                    _                       => 1
                };
            }
            return Score(c1) >= Score(c2) ? c1 : c2;
        }

        static int AiBid(Player ai, int currentItemPrice, int myCardDiscount)
        {
            if (ai.IsEliminated || ai.SkipThisRound) return 0;
            int afford = ai.Money - 50; // keep Rs.50 buffer
            int maxWilling = Math.Min(afford, currentItemPrice + rng.Next(0, 81));
            if (maxWilling < 50) return 0; // pass
            return Math.Max(50, maxWilling - myCardDiscount);
        }

        // ── Card Resolution ──────────────────────────────────

        static int ResolveCards(
            Player shopkeeper, List<Player> customers, Player humanPlayer,
            int basePrice, bool noDiscountActive, bool stealBackActive,
            RoundResult result, List<Player> frozenPlayers, List<Player> allPlayers)
        {
            int adjustedPrice = basePrice;
            int bluffOffset = 0;

            // ── Shopkeeper card ──────────────────────────────
            var skCard = shopkeeper.PlayedCard;
            if (skCard != null)
            {
                PrintLine($"\n  [SHOPKEEPER] plays: {skCard.Name}", ConsoleColor.Yellow);

                switch (skCard.Type)
                {
                    case CardType.BluffPrice:
                        bluffOffset = (rng.Next(0, 2) == 0) ? 50 : -50;
                        int displayPrice = basePrice + bluffOffset;
                        if (displayPrice < 50) displayPrice = 50;
                        result.EventLog.Add($"Bluff Price active! Displayed: Rs.{displayPrice}, Real: Rs.{basePrice}");
                        PrintLine($"  Displayed price is Rs.{displayPrice}. Real price hidden until purchase!", ConsoleColor.DarkYellow);
                        adjustedPrice = displayPrice; // bidders see this
                        break;

                    case CardType.FlashTax:
                        // Pick up to 2 customers (AI picks highest bidders; human picks if they are SK)
                        var taxTargets = customers.Where(c => !c.IsEliminated)
                                                  .OrderByDescending(c => c.Money)
                                                  .Take(rng.Next(1, 3)).ToList();
                        if (shopkeeper.IsHuman)
                        {
                            taxTargets = HumanPickTargets(customers, 2, "Flash Tax (pick 1–2 to tax Rs.50)");
                        }
                        foreach (var t in taxTargets)
                        {
                            int tax = Math.Min(50, t.Money);
                            t.Money -= tax;
                            shopkeeper.Money += tax;
                            result.EventLog.Add($"{t.Name} paid Rs.{tax} Flash Tax to {shopkeeper.Name}.");
                            PrintLine($"  {t.Name} pays Rs.{tax} Flash Tax!", ConsoleColor.Red);
                        }
                        break;

                    case CardType.BiddingWar:
                        result.EventLog.Add("Bidding War: all must bid ≥Rs.50 or pay Rs.30.");
                        PrintLine("  Bidding War! Everyone must bid ≥Rs.50 or pay Rs.30 penalty.", ConsoleColor.DarkYellow);
                        break;

                    case CardType.PriceHike:
                        result.EventLog.Add("Price Hike announced! Next round item +Rs.50.");
                        PrintLine("  PRICE HIKE! Next round's item will cost Rs.50 more!", ConsoleColor.DarkYellow);
                        // Flag is set in main game loop
                        break;

                    case CardType.LoyaltyPenalty:
                        var richest = customers.Where(c => !c.IsEliminated)
                                               .OrderByDescending(c => c.ItemsWon).FirstOrDefault();
                        if (richest != null && richest.ItemsWon > 0)
                        {
                            int pen = Math.Min(80, richest.Money);
                            richest.Money -= pen;
                            shopkeeper.Money += pen;
                            result.EventLog.Add($"{richest.Name} (most items) paid Rs.{pen} Loyalty Penalty.");
                            PrintLine($"  {richest.Name} pays Rs.{pen} Loyalty Penalty!", ConsoleColor.Red);
                        }
                        else PrintLine("  No effect — nobody has won an item yet.", ConsoleColor.DarkGray);
                        break;

                    case CardType.AuctionFreeze:
                        Player freezeTarget;
                        if (shopkeeper.IsHuman)
                            freezeTarget = HumanPickTargets(customers, 1, "Auction Freeze (pick 1 to cancel)").FirstOrDefault();
                        else
                            freezeTarget = customers.Where(c => !c.IsEliminated).OrderByDescending(c => c.Money).FirstOrDefault();
                        if (freezeTarget != null)
                        {
                            frozenPlayers.Add(freezeTarget);
                            result.EventLog.Add($"{freezeTarget.Name}'s card frozen by Auction Freeze.");
                            PrintLine($"  {freezeTarget.Name}'s card is FROZEN — no effect!", ConsoleColor.DarkYellow);
                        }
                        break;

                    case CardType.NoDiscountZone:
                        result.EventLog.Add("No Discount Zone: next round price-cut cards blocked.");
                        PrintLine("  No Discount Zone set! Next round: price-cut cards won't work.", ConsoleColor.DarkYellow);
                        break;

                    case CardType.StealBack:
                        result.EventLog.Add("Steal Back active! Any steal cards will backfire.");
                        PrintLine("  Steal Back active! Stealers will pay Rs.60 instead.", ConsoleColor.DarkYellow);
                        break;

                    case CardType.DoubleOrNothing:
                        result.EventLog.Add("Double or Nothing! Winner pays 2× bid.");
                        PrintLine("  Double or Nothing! Winner will pay 2× their bid!", ConsoleColor.Magenta);
                        break;
                }
            }

            // ── Customer cards ───────────────────────────────
            foreach (var customer in customers)
            {
                if (customer.IsEliminated) continue;
                var card = customer.PlayedCard;
                if (card == null) continue;
                if (frozenPlayers.Contains(customer))
                {
                    PrintLine($"\n  [{customer.Name}] plays: {card.Name} — FROZEN, no effect.", ConsoleColor.DarkGray);
                    continue;
                }

                PrintLine($"\n  [{customer.Name}] plays: {card.Name}", ConsoleColor.Cyan);

                switch (card.Type)
                {
                    case CardType.SalaryDay:
                        customer.Money += 100;
                        result.EventLog.Add($"{customer.Name} gained Rs.100 (Salary Day). Now Rs.{customer.Money}.");
                        PrintLine($"  +Rs.100 added to {customer.Name}'s budget!", ConsoleColor.Green);
                        break;

                    case CardType.Pickpocket:
                        // Resolved after bids — flagged here
                        result.EventLog.Add($"{customer.Name} has Pickpocket active.");
                        PrintLine($"  Pickpocket active! Anyone bidding >Rs.200 will lose Rs.50.", ConsoleColor.DarkYellow);
                        break;

                    case CardType.Negotiator:
                        result.EventLog.Add($"{customer.Name} has Negotiator: -Rs.70 if they win.");
                        PrintLine($"  Negotiator: {customer.Name} pays Rs.70 less if they win.", ConsoleColor.Green);
                        break;

                    case CardType.Heist:
                        if (stealBackActive)
                        {
                            int sb = Math.Min(60, customer.Money);
                            customer.Money -= sb;
                            shopkeeper.Money += sb;
                            result.EventLog.Add($"{customer.Name} Heist backfired! Paid Rs.{sb} to shopkeeper.");
                            PrintLine($"  STEAL BACK! {customer.Name}'s Heist failed — paid Rs.{sb} to shopkeeper.", ConsoleColor.Red);
                        }
                        else
                        {
                            Player heistTarget;
                            if (customer.IsHuman)
                                heistTarget = HumanPickTargets(customers.Where(c => c != customer && !c.IsEliminated).ToList(), 1, "Heist: pick target").FirstOrDefault();
                            else
                                heistTarget = customers.Where(c => c != customer && !c.IsEliminated)
                                                       .OrderByDescending(c => c.Money).FirstOrDefault();
                            if (heistTarget != null)
                            {
                                int roll = RollDie($"Heist by {customer.Name}");
                                if (roll >= 3)
                                {
                                    int stolen = Math.Min(80, heistTarget.Money);
                                    heistTarget.Money -= stolen;
                                    customer.Money += stolen;
                                    result.EventLog.Add($"{customer.Name} stole Rs.{stolen} from {heistTarget.Name} (Heist, roll {roll}).");
                                    PrintLine($"  SUCCESS! {customer.Name} stole Rs.{stolen} from {heistTarget.Name}!", ConsoleColor.Green);
                                }
                                else
                                {
                                    int paid = Math.Min(40, customer.Money);
                                    customer.Money -= paid;
                                    heistTarget.Money += paid;
                                    result.EventLog.Add($"{customer.Name}'s Heist failed (roll {roll}). Paid Rs.{paid} to {heistTarget.Name}.");
                                    PrintLine($"  FAILED! {customer.Name} paid Rs.{paid} to {heistTarget.Name}.", ConsoleColor.Red);
                                }
                            }
                        }
                        break;

                    case CardType.PriceDrop:
                        if (noDiscountActive)
                        {
                            result.EventLog.Add($"{customer.Name}'s Price Drop blocked (No Discount Zone).");
                            PrintLine($"  Price Drop BLOCKED by No Discount Zone!", ConsoleColor.DarkRed);
                        }
                        else
                        {
                            adjustedPrice = Math.Max(50, adjustedPrice - 60);
                            result.EventLog.Add($"{customer.Name} used Price Drop. Price now Rs.{adjustedPrice}.");
                            PrintLine($"  Price drops Rs.60! New price: Rs.{adjustedPrice}", ConsoleColor.Green);
                        }
                        break;

                    case CardType.MarketResearch:
                        customer.SkipThisRound = true;
                        customer.NextRoundSeeRealPrice = true;
                        customer.NextRoundBonusMoney += 60;
                        result.EventLog.Add($"{customer.Name} skips this round (Market Research). Gets +Rs.60 + real price view next round.");
                        PrintLine($"  {customer.Name} skips bidding. Gets Rs.60 + real price peek next round.", ConsoleColor.DarkCyan);
                        break;

                    case CardType.Sabotage:
                        Player sabTarget;
                        if (customer.IsHuman)
                            sabTarget = HumanPickTargets(customers.Where(c => c != customer && !c.IsEliminated).ToList(), 1, "Sabotage: pick target").FirstOrDefault();
                        else
                            sabTarget = customers.Where(c => c != customer && !c.IsEliminated)
                                                 .OrderByDescending(c => c.Money).FirstOrDefault();
                        if (sabTarget != null && !frozenPlayers.Contains(sabTarget))
                        {
                            frozenPlayers.Add(sabTarget);
                            result.EventLog.Add($"{customer.Name} sabotaged {sabTarget.Name}'s card.");
                            PrintLine($"  {sabTarget.Name}'s card SABOTAGED — no effect!", ConsoleColor.DarkYellow);
                        }
                        break;

                    case CardType.GroupPressure:
                        if (noDiscountActive)
                        {
                            PrintLine($"  Group Pressure BLOCKED by No Discount Zone!", ConsoleColor.DarkRed);
                            break;
                        }
                        ResolveGroupPressure(customers, customer, ref adjustedPrice, result, allPlayers);
                        break;

                    case CardType.ItemSnatch:
                        // Resolved after winner is decided — flagged
                        result.EventLog.Add($"{customer.Name} has Item Snatch ready.");
                        PrintLine($"  Item Snatch ready! Will activate after winner is decided.", ConsoleColor.DarkYellow);
                        break;
                }
            }

            return adjustedPrice;
        }

        static void ResolveGroupPressure(List<Player> customers, Player initiator,
                                          ref int price, RoundResult result, List<Player> allPlayers)
        {
            PrintLine("\n  GROUP PRESSURE! All customers vote (secretly)...", ConsoleColor.Magenta);
            var active = customers.Where(c => !c.IsEliminated && !c.SkipThisRound).ToList();
            bool allAgree = true;
            var defectors = new List<Player>();

            foreach (var c in active)
            {
                bool vote;
                if (c.IsHuman)
                {
                    Print($"  {c.Name}, vote (Y=agree / N=defect): ", ConsoleColor.Cyan);
                    string v = Console.ReadLine()?.Trim().ToUpper();
                    vote = v == "Y";
                }
                else
                {
                    vote = rng.Next(0, 3) != 0; // AI agrees 67% of time
                }
                if (!vote) { allAgree = false; defectors.Add(c); }
            }

            if (allAgree)
            {
                price = Math.Max(50, price - 100);
                result.EventLog.Add($"Group Pressure unanimous! Price dropped to Rs.{price}.");
                PrintLine($"  UNANIMOUS! Item price drops Rs.100. New price: Rs.{price}", ConsoleColor.Green);
            }
            else
            {
                foreach (var d in defectors)
                {
                    d.Money += 50;
                    result.EventLog.Add($"{d.Name} defected — gained Rs.50.");
                    PrintLine($"  {d.Name} DEFECTED! +Rs.50 for them.", ConsoleColor.DarkYellow);
                }
                foreach (var c in active.Except(defectors))
                {
                    int loss = Math.Min(30, c.Money);
                    c.Money -= loss;
                    result.EventLog.Add($"{c.Name} was betrayed — lost Rs.{loss}.");
                    PrintLine($"  {c.Name} betrayed! -Rs.{loss}.", ConsoleColor.Red);
                }
            }
        }

        static List<Player> HumanPickTargets(List<Player> pool, int max, string prompt)
        {
            var result = new List<Player>();
            var available = pool.Where(p => !p.IsEliminated).ToList();
            if (!available.Any()) return result;

            PrintLine($"\n  {prompt}", ConsoleColor.Yellow);
            for (int i = 0; i < available.Count; i++)
                PrintLine($"  {i + 1}. {available[i].Name}  (Rs.{available[i].Money})", ConsoleColor.Gray);

            for (int n = 0; n < max; n++)
            {
                if (n >= available.Count) break;
                Print($"  Pick #{n + 1} (1–{available.Count}, or 0 to stop): ", ConsoleColor.Cyan);
                if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= available.Count)
                    result.Add(available[choice - 1]);
                else break;
            }
            return result;
        }

        // ── Bidding Phase ────────────────────────────────────

        static void BiddingPhase(List<Player> customers, Player shopkeeper,
                                  int adjustedPrice, bool biddingWarActive,
                                  bool doubleOrNothing, RoundResult result)
        {
            PrintLine("\n  ── BIDDING PHASE ──────────────────────────────", ConsoleColor.DarkGray);
            PrintLine($"  Current item price: Rs.{adjustedPrice}", ConsoleColor.White);
            Console.WriteLine();

            var bids = new Dictionary<Player, int>();

            foreach (var customer in customers)
            {
                if (customer.IsEliminated || customer.SkipThisRound)
                {
                    PrintLine($"  {customer.Name}: SKIPPING", ConsoleColor.DarkGray);
                    bids[customer] = 0;
                    continue;
                }

                int myDiscount = customer.PlayedCard?.Type == CardType.Negotiator ? 70 : 0;

                if (customer.IsHuman)
                {
                    PrintLine($"  Your budget: Rs.{customer.Money}", ConsoleColor.Cyan);
                    Print($"  Your bid (0 to pass): Rs.", ConsoleColor.Cyan);
                    if (int.TryParse(Console.ReadLine(), out int humanBid) && humanBid > 0 && humanBid <= customer.Money)
                    {
                        if (biddingWarActive && humanBid < 50)
                        {
                            PrintLine("  Too low! Bidding War forces min Rs.50. Paying Rs.30 penalty.", ConsoleColor.Red);
                            customer.Money -= 30;
                            shopkeeper.Money += 30;
                            bids[customer] = 0;
                        }
                        else
                        {
                            bids[customer] = humanBid;
                            PrintLine($"  {customer.Name} bids Rs.{humanBid}", ConsoleColor.Cyan);
                        }
                    }
                    else
                    {
                        if (biddingWarActive)
                        {
                            int pen = Math.Min(30, customer.Money);
                            customer.Money -= pen;
                            shopkeeper.Money += pen;
                            result.EventLog.Add($"{customer.Name} passed under Bidding War — Rs.{pen} penalty.");
                            PrintLine($"  Passed! Rs.{pen} penalty paid to shopkeeper.", ConsoleColor.Red);
                        }
                        else PrintLine($"  {customer.Name} passes.", ConsoleColor.DarkGray);
                        bids[customer] = 0;
                    }
                }
                else
                {
                    int aiBid = AiBid(customer, adjustedPrice, myDiscount);
                    if (biddingWarActive && aiBid < 50 && aiBid > 0) aiBid = 50;
                    if (biddingWarActive && aiBid == 0)
                    {
                        int pen = Math.Min(30, customer.Money);
                        customer.Money -= pen;
                        shopkeeper.Money += pen;
                        result.EventLog.Add($"{customer.Name} passed under Bidding War — Rs.{pen} penalty.");
                        PrintLine($"  {customer.Name} passes (penalty Rs.{pen}).", ConsoleColor.Red);
                    }
                    else
                    {
                        PrintLine($"  {customer.Name} bids Rs.{aiBid}", ConsoleColor.Gray);
                    }
                    bids[customer] = aiBid;
                }
            }

            // Determine winner
            var topBid = bids.Where(b => b.Value > 0).OrderByDescending(b => b.Value).FirstOrDefault();

            if (topBid.Key == null)
            {
                result.EventLog.Add("No bids — item unsold.");
                PrintLine("\n  No bids! Item goes UNSOLD.", ConsoleColor.DarkRed);
                result.Winner = null;
                return;
            }

            var winner = topBid.Key;
            int winBid = topBid.Value;

            // Pickpocket resolution
            foreach (var c in customers.Where(c => !c.IsEliminated && c.PlayedCard?.Type == CardType.Pickpocket))
            {
                foreach (var b in bids.Where(b => b.Value > 200 && b.Key != c))
                {
                    if (shopkeeper.PlayedCard?.Type == CardType.StealBack)
                    {
                        int sbfee = Math.Min(60, c.Money);
                        c.Money -= sbfee;
                        shopkeeper.Money += sbfee;
                        result.EventLog.Add($"{c.Name} Pickpocket backfired! Rs.{sbfee} to shopkeeper.");
                        PrintLine($"  STEAL BACK! {c.Name}'s Pickpocket backfired — Rs.{sbfee} to shopkeeper.", ConsoleColor.Red);
                    }
                    else
                    {
                        int stolen = Math.Min(50, b.Key.Money);
                        b.Key.Money -= stolen;
                        c.Money += stolen;
                        result.EventLog.Add($"{c.Name} pickpocketed Rs.{stolen} from {b.Key.Name}.");
                        PrintLine($"  PICKPOCKET! {c.Name} steals Rs.{stolen} from {b.Key.Name}!", ConsoleColor.DarkYellow);
                    }
                }
            }

            // Negotiator discount
            if (winner.PlayedCard?.Type == CardType.Negotiator)
            {
                winBid = Math.Max(50, winBid - 70);
                result.EventLog.Add($"{winner.Name} used Negotiator. Pays Rs.{winBid} instead.");
                PrintLine($"  Negotiator! {winner.Name} pays Rs.{winBid} instead.", ConsoleColor.Green);
            }

            // Double or Nothing
            if (doubleOrNothing)
            {
                winBid *= 2;
                result.EventLog.Add($"Double or Nothing! {winner.Name} pays Rs.{winBid}.");
                PrintLine($"  DOUBLE OR NOTHING! {winner.Name} pays Rs.{winBid}!", ConsoleColor.Magenta);
            }

            // Can winner afford?
            if (winBid > winner.Money)
            {
                result.EventLog.Add($"{winner.Name} can't afford Rs.{winBid}. Forfeit.");
                PrintLine($"  {winner.Name} can't afford Rs.{winBid}! Forfeiting...", ConsoleColor.Red);
                var second = bids.Where(b => b.Key != winner && b.Value > 0)
                                 .OrderByDescending(b => b.Value).FirstOrDefault();
                if (second.Key == null)
                {
                    result.Winner = null;
                    PrintLine("  No other bidder — item unsold.", ConsoleColor.DarkRed);
                    return;
                }
                winner = second.Key;
                winBid = second.Value;
                PrintLine($"  {winner.Name} wins instead with Rs.{winBid}!", ConsoleColor.Cyan);
            }

            result.Winner   = winner;
            result.WinningBid = winBid;

            Console.WriteLine();
            PrintLine($"  WINNER: {winner.Name} with bid Rs.{winBid}!", ConsoleColor.Green);
        }

        // ── Item Snatch ──────────────────────────────────────

        static void ResolveItemSnatch(List<Player> customers, Player shopkeeper,
                                       int basePrice, RoundResult result)
        {
            var snatchers = customers.Where(c =>
                !c.IsEliminated && !c.SkipThisRound &&
                c.PlayedCard?.Type == CardType.ItemSnatch &&
                c != result.Winner).ToList();

            if (!snatchers.Any() || result.Winner == null) return;

            // Only first snatcher gets the attempt
            var snatcher = snatchers.First();
            PrintLine($"\n  {snatcher.Name} attempts Item Snatch!", ConsoleColor.Magenta);

            if (shopkeeper.PlayedCard?.Type == CardType.StealBack)
            {
                int fee = Math.Min(60, snatcher.Money);
                snatcher.Money -= fee;
                shopkeeper.Money += fee;
                result.EventLog.Add($"{snatcher.Name}'s Item Snatch backfired — Rs.{fee} to shopkeeper.");
                PrintLine($"  STEAL BACK! Snatch failed — Rs.{fee} to shopkeeper.", ConsoleColor.Red);
                return;
            }

            int roll = RollDie($"Item Snatch by {snatcher.Name}");
            if (roll >= 4)
            {
                int pay = Math.Min(basePrice, snatcher.Money);
                snatcher.Money -= pay;
                shopkeeper.Money += pay;
                // Transfer item
                if (result.Winner != null)
                {
                    result.Winner.ItemsWon = Math.Max(0, result.Winner.ItemsWon - 1);
                    // Refund winner's bid
                    result.Winner.Money += result.WinningBid;
                    shopkeeper.Money -= result.WinningBid;
                }
                snatcher.ItemsWon++;
                result.EventLog.Add($"{snatcher.Name} SNATCHED item from {result.Winner?.Name}! Paid base price Rs.{pay}.");
                PrintLine($"  SUCCESS! {snatcher.Name} snatched the item! Paid Rs.{pay}.", ConsoleColor.Green);
                result.ItemSnatched = true;
                result.Snatcher = snatcher;
            }
            else
            {
                int comp = Math.Min(60, snatcher.Money);
                snatcher.Money -= comp;
                result.Winner.Money += comp;
                result.EventLog.Add($"{snatcher.Name}'s snatch failed (roll {roll}). Paid Rs.{comp} compensation.");
                PrintLine($"  FAILED! {snatcher.Name} pays Rs.{comp} to {result.Winner.Name}.", ConsoleColor.Red);
            }
        }

        // ── Main Game Loop ───────────────────────────────────

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "BAZAR — Card Game";
            Console.Clear();

            // Title screen
            Header("  B A Z A R", ConsoleColor.Yellow);
            PrintLine("  A classroom auction card game", ConsoleColor.DarkGray);
            PrintLine("  1 Shopkeeper  vs  3 Customers", ConsoleColor.DarkGray);
            Console.WriteLine();
            Pause("  Press ENTER to read the rules...");

            ShowRules();

            // Role selection
            Console.Clear();
            Header("CHOOSE YOUR ROLE", ConsoleColor.Cyan);
            PrintLine("  1. Shopkeeper  — Set prices, bluff, tax customers.", ConsoleColor.Yellow);
            PrintLine("  2. Customer    — Bid on items, steal, negotiate.", ConsoleColor.Cyan);
            Console.WriteLine();
            Print("  Enter 1 or 2: ", ConsoleColor.White);
            int roleChoice = 0;
            while (roleChoice != 1 && roleChoice != 2)
                int.TryParse(Console.ReadLine(), out roleChoice);

            bool humanIsShopkeeper = roleChoice == 1;
            Console.WriteLine();

            // Build players
            string humanName;
            Print("  Enter your name: ", ConsoleColor.White);
            humanName = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(humanName)) humanName = "Player";

            var shopkeeper = humanIsShopkeeper
                ? new Player(humanName, true, true, 0)
                : new Player("Ahmed", false, true, 0);

            var customers = new List<Player>();
            if (!humanIsShopkeeper) customers.Add(new Player(humanName, true, false, 400));
            customers.Add(new Player("Sara",  false, false, 400));
            customers.Add(new Player("Bilal", false, false, 400));
            if (humanIsShopkeeper)  customers.Add(new Player("Zara", false, false, 400));

            var allPlayers = new List<Player> { shopkeeper };
            allPlayers.AddRange(customers);

            // Game state
            bool nextRoundPriceHike   = false;
            bool nextRoundNoDiscount  = false;
            int  unsoldCount          = 0;
            var  roundSummaries       = new List<(string item, string winner, int bid, List<string> log)>();

            // ── 5 Rounds ─────────────────────────────────────
            for (int round = 1; round <= 5; round++)
            {
                Console.Clear();
                Header($"ROUND {round} OF 5  —  {ItemNames[round - 1].ToUpper()}", ConsoleColor.Yellow);

                // Apply carry-over bonuses
                foreach (var p in customers)
                {
                    if (p.NextRoundBonusMoney > 0)
                    {
                        p.Money += p.NextRoundBonusMoney;
                        PrintLine($"  {p.Name} receives carry-over bonus +Rs.{p.NextRoundBonusMoney}!", ConsoleColor.Green);
                        p.NextRoundBonusMoney = 0;
                    }
                }

                // Item price
                int basePrice;
                string itemName = ItemNames[round - 1];
                if (nextRoundPriceHike) { Console.Write(""); } // hint only

                if (humanIsShopkeeper)
                {
                    PrintLine($"  Item: {itemName}", ConsoleColor.White);
                    if (nextRoundPriceHike) PrintLine("  (Price Hike active — add Rs.50 to your price!)", ConsoleColor.DarkYellow);
                    Print($"  Set your price (100–200): Rs.", ConsoleColor.Yellow);
                    while (!int.TryParse(Console.ReadLine(), out basePrice) || basePrice < 50 || basePrice > 300)
                        Print("  Enter a price Rs.50–300: Rs.", ConsoleColor.Yellow);
                    if (nextRoundPriceHike) basePrice += 50;
                }
                else
                {
                    basePrice = rng.Next(100, 201);
                    if (nextRoundPriceHike) basePrice += 50;
                    PrintLine($"  Item: {itemName}  |  Shopkeeper's price: Rs.{basePrice}", ConsoleColor.White);
                }
                nextRoundPriceHike = false;

                // Customers who can see real price (Market Research carry-over)
                foreach (var c in customers.Where(c => c.NextRoundSeeRealPrice))
                {
                    PrintLine($"  {c.Name} sees the real price: Rs.{basePrice} (Market Research)", ConsoleColor.DarkCyan);
                    c.NextRoundSeeRealPrice = false;
                }

                ShowMoney(allPlayers);

                // ── Draw cards ───────────────────────────────
                PrintLine("  ── DRAWING CARDS ─────────────────────────────", ConsoleColor.DarkGray);
                Console.WriteLine();

                // Shopkeeper
                shopkeeper.DrawnCard1 = DrawCard(true);
                shopkeeper.DrawnCard2 = DrawCard(true);

                if (humanIsShopkeeper)
                {
                    PrintLine("  Your two cards:", ConsoleColor.Yellow);
                    Print("  [1] ", ConsoleColor.Yellow); DisplayCard(shopkeeper.DrawnCard1);
                    Print("  [2] ", ConsoleColor.Yellow); DisplayCard(shopkeeper.DrawnCard2);
                    Print("  Choose card (1 or 2): ", ConsoleColor.Yellow);
                    int pick = 0;
                    while (pick != 1 && pick != 2) int.TryParse(Console.ReadLine(), out pick);
                    shopkeeper.PlayedCard = pick == 1 ? shopkeeper.DrawnCard1 : shopkeeper.DrawnCard2;
                    PrintLine($"  Playing: {shopkeeper.PlayedCard.Name}", ConsoleColor.Yellow);
                }
                else
                {
                    shopkeeper.PlayedCard = AiPickCard(shopkeeper, shopkeeper.DrawnCard1, shopkeeper.DrawnCard2,
                                                        basePrice, customers, shopkeeper);
                    PrintLine($"  {shopkeeper.Name} (Shopkeeper) draws cards... [face down]", ConsoleColor.DarkGray);
                }

                Console.WriteLine();

                // Customers
                foreach (var c in customers)
                {
                    if (c.IsEliminated) { PrintLine($"  {c.Name} is ELIMINATED.", ConsoleColor.DarkRed); continue; }

                    c.DrawnCard1 = DrawCard(false);
                    c.DrawnCard2 = DrawCard(false);

                    if (c.IsHuman)
                    {
                        PrintLine($"  Your two cards:", ConsoleColor.Cyan);
                        Print("  [1] ", ConsoleColor.Cyan); DisplayCard(c.DrawnCard1);
                        Print("  [2] ", ConsoleColor.Cyan); DisplayCard(c.DrawnCard2);
                        Print("  Choose card (1 or 2): ", ConsoleColor.Cyan);
                        int pick = 0;
                        while (pick != 1 && pick != 2) int.TryParse(Console.ReadLine(), out pick);
                        c.PlayedCard = pick == 1 ? c.DrawnCard1 : c.DrawnCard2;
                        PrintLine($"  Playing: {c.PlayedCard.Name}", ConsoleColor.Cyan);
                    }
                    else
                    {
                        c.PlayedCard = AiPickCard(c, c.DrawnCard1, c.DrawnCard2, basePrice, customers, shopkeeper);
                        PrintLine($"  {c.Name} picks a card... [face down]", ConsoleColor.DarkGray);
                    }
                }

                Pause("  Cards chosen. Press ENTER to reveal...");

                // ── Reveal cards ─────────────────────────────
                Header("CARD REVEAL", ConsoleColor.Magenta);

                Print($"  {shopkeeper.Name} (Shopkeeper): ", ConsoleColor.Yellow);
                Console.WriteLine();
                if (shopkeeper.PlayedCard != null) DisplayCard(shopkeeper.PlayedCard);

                foreach (var c in customers)
                {
                    if (c.IsEliminated) continue;
                    Print($"  {c.Name}: ", ConsoleColor.Cyan);
                    Console.WriteLine();
                    if (c.PlayedCard != null) DisplayCard(c.PlayedCard);
                }

                Pause("  Press ENTER to resolve cards...");

                // ── Resolve cards ────────────────────────────
                Header("CARD EFFECTS", ConsoleColor.DarkYellow);

                var result    = new RoundResult { ItemName = itemName, ActualPrice = basePrice };
                var frozen    = new List<Player>();
                bool stealBackActive  = shopkeeper.PlayedCard?.Type == CardType.StealBack;
                bool doubleOrNothing  = shopkeeper.PlayedCard?.Type == CardType.DoubleOrNothing;
                bool biddingWarActive = shopkeeper.PlayedCard?.Type == CardType.BiddingWar;

                int adjustedPrice = ResolveCards(shopkeeper, customers, null,
                                                  basePrice, nextRoundNoDiscount, stealBackActive,
                                                  result, frozen, allPlayers);

                if (shopkeeper.PlayedCard?.Type == CardType.PriceHike) nextRoundPriceHike = true;
                if (shopkeeper.PlayedCard?.Type == CardType.NoDiscountZone) nextRoundNoDiscount = true;
                else nextRoundNoDiscount = false;

                Pause("  Press ENTER to start bidding...");

                // ── Bidding ───────────────────────────────────
                BiddingPhase(customers, shopkeeper, adjustedPrice,
                              biddingWarActive, doubleOrNothing, result);

                // ── Item Snatch ───────────────────────────────
                if (result.Winner != null)
                {
                    result.Winner.ItemsWon++;
                    int pay = Math.Min(result.WinningBid, result.Winner.Money);
                    result.Winner.Money -= pay;
                    shopkeeper.Money    += pay;
                    result.EventLog.Add($"{result.Winner.Name} wins {itemName} for Rs.{pay}.");

                    // Bluff price real reveal
                    if (shopkeeper.PlayedCard?.Type == CardType.BluffPrice)
                    {
                        PrintLine($"\n  BLUFF REVEAL! Real price was Rs.{basePrice}.", ConsoleColor.Magenta);
                        int diff = basePrice - adjustedPrice;
                        if (diff > 0)
                        {
                            int extra = Math.Min(diff, result.Winner.Money);
                            result.Winner.Money -= extra;
                            shopkeeper.Money    += extra;
                            PrintLine($"  {result.Winner.Name} pays extra Rs.{extra}.", ConsoleColor.Red);
                        }
                        else if (diff < 0)
                        {
                            int refund = Math.Abs(diff);
                            shopkeeper.Money -= refund;
                            result.Winner.Money += refund;
                            PrintLine($"  {result.Winner.Name} gets Rs.{refund} refund!", ConsoleColor.Green);
                        }
                    }

                    ResolveItemSnatch(customers, shopkeeper, basePrice, result);
                }
                else
                {
                    unsoldCount++;
                }

                // Elimination check
                foreach (var p in customers.Where(p => !p.IsEliminated && p.Money < 50))
                {
                    p.IsEliminated = true;
                    PrintLine($"\n  {p.Name} is ELIMINATED — budget below Rs.50!", ConsoleColor.DarkRed);
                    result.EventLog.Add($"{p.Name} eliminated.");
                }

                // ── Round Summary ─────────────────────────────
                Pause("  Press ENTER for round summary...");
                Header($"ROUND {round} SUMMARY", ConsoleColor.Green);

                string winnerName = result.ItemSnatched
                    ? $"{result.Snatcher.Name} (SNATCHED)"
                    : result.Winner != null ? result.Winner.Name : "UNSOLD";

                PrintLine($"  Item:    {itemName}", ConsoleColor.White);
                PrintLine($"  Winner:  {winnerName}", ConsoleColor.Green);
                if (result.Winner != null)
                    PrintLine($"  Bid:     Rs.{result.WinningBid}", ConsoleColor.White);
                Console.WriteLine();

                PrintLine("  Cards played this round:", ConsoleColor.DarkGray);
                PrintLine($"  {shopkeeper.Name,-18} → {shopkeeper.PlayedCard?.Name ?? "none"}", ConsoleColor.Yellow);
                foreach (var c in customers)
                    PrintLine($"  {c.Name,-18} → {c.PlayedCard?.Name ?? "none"}", ConsoleColor.Cyan);

                Console.WriteLine();
                PrintLine("  Events:", ConsoleColor.DarkGray);
                foreach (var ev in result.EventLog)
                    PrintLine($"  • {ev}", ConsoleColor.Gray);

                ShowMoney(allPlayers);
                roundSummaries.Add((itemName, winnerName, result.WinningBid, result.EventLog));

                Pause("  Press ENTER for next round...");
            }

            // ── Final Results ─────────────────────────────────
            Console.Clear();
            Header("FINAL RESULTS", ConsoleColor.Yellow);

            PrintLine("  ROUND-BY-ROUND RECAP", ConsoleColor.DarkGray);
            Divider('─', 60, ConsoleColor.DarkGray);
            for (int i = 0; i < roundSummaries.Count; i++)
            {
                var (item, winner, bid, _) = roundSummaries[i];
                string bidStr = bid > 0 ? $"Rs.{bid}" : "—";
                PrintLine($"  Round {i + 1}  {item,-18}  Winner: {winner,-20}  Bid: {bidStr}", ConsoleColor.Gray);
            }
            Console.WriteLine();

            Divider('─', 60, ConsoleColor.DarkGray);
            PrintLine("  FINAL STANDINGS", ConsoleColor.White);
            Divider('─', 60, ConsoleColor.DarkGray);

            PrintLine($"  {shopkeeper.Name,-20} Rs.{shopkeeper.Money,5}   [Shopkeeper]", ConsoleColor.Yellow);
            foreach (var c in customers.OrderByDescending(c => c.ItemsWon).ThenByDescending(c => c.Money))
            {
                string status = c.IsEliminated ? " ELIMINATED" : $"  Items: {c.ItemsWon}";
                ConsoleColor col = c.IsEliminated ? ConsoleColor.DarkRed : ConsoleColor.Cyan;
                PrintLine($"  {c.Name,-20} Rs.{c.Money,5}  {status}", col);
            }

            Console.WriteLine();
            Divider('═', 60, ConsoleColor.DarkGray);

            // Shopkeeper result
            PrintLine("\n  SHOPKEEPER VERDICT", ConsoleColor.Yellow);
            if (shopkeeper.Money >= 800)
                PrintLine($"  WIN!  {shopkeeper.Name} earned Rs.{shopkeeper.Money} — target Rs.800 met!", ConsoleColor.Green);
            else if (shopkeeper.Money >= 500)
                PrintLine($"  DRAW. {shopkeeper.Name} earned Rs.{shopkeeper.Money} — survived but not thriving.", ConsoleColor.DarkYellow);
            else
                PrintLine($"  LOSS. {shopkeeper.Name} earned only Rs.{shopkeeper.Money} — under Rs.500 target.", ConsoleColor.Red);

            if (unsoldCount >= 3)
                PrintLine($"  Also: {unsoldCount} items went unsold — automatic loss condition.", ConsoleColor.DarkRed);

            // Customer result
            Console.WriteLine();
            PrintLine("  CUSTOMER VERDICT", ConsoleColor.Cyan);
            var topCustomer = customers.Where(c => !c.IsEliminated)
                                       .OrderByDescending(c => c.ItemsWon)
                                       .ThenByDescending(c => c.Money)
                                       .FirstOrDefault();
            if (topCustomer != null)
                PrintLine($"  WINNER: {topCustomer.Name} — {topCustomer.ItemsWon} item(s), Rs.{topCustomer.Money} left!", ConsoleColor.Green);
            else
                PrintLine("  All customers were eliminated!", ConsoleColor.DarkRed);

            Console.WriteLine();
            Divider('═', 60, ConsoleColor.Yellow);
            PrintLine("  Thanks for playing BAZAR!", ConsoleColor.DarkGray);
            Console.WriteLine();
        }
    }
}