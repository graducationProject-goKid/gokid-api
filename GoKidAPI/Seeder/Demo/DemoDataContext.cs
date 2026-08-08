namespace GoKidAPI.Seeder.Demo
{
    // Shared randomness, name pools and helpers used by every Demo* seeder.
    // Not a DI service on purpose - matches the existing static-seeder style.
    public static class DemoDataContext
    {
        public static readonly Random Rng = new Random();

        public enum ActivityTier
        {
            Beginner,
            Casual,
            Active,
            StarPerformer,
            Veteran
        }

        public static ActivityTier RollTier()
        {
            var roll = Rng.NextDouble();
            if (roll < 0.20) return ActivityTier.Beginner;
            if (roll < 0.50) return ActivityTier.Casual;
            if (roll < 0.80) return ActivityTier.Active;
            if (roll < 0.95) return ActivityTier.StarPerformer;
            return ActivityTier.Veteran;
        }

        public static readonly string[] BoyFirstNames =
        {
            "Ahmed", "Youssef", "Omar", "Karim", "Ali", "Mostafa", "Hassan", "Hussein", "Ziad", "Adam",
            "Mahmoud", "Khaled", "Amir", "Tarek", "Sami", "Zein", "Yassin", "Marwan", "Seif", "Anas",
            "Ibrahim", "Nour El-Din", "Rayan", "Hamza", "Malek"
        };

        public static readonly string[] GirlFirstNames =
        {
            "Mariam", "Nour", "Hana", "Laila", "Salma", "Farida", "Malak", "Jana", "Yasmin", "Aya",
            "Rana", "Lina", "Sarah", "Habiba", "Nada", "Dina", "Rawan", "Zeina", "Alia", "Maya",
            "Reem", "Roqaia", "Sondos", "Menna", "Fatima"
        };

        public static readonly string[] FamilyNames =
        {
            "El-Sayed", "Hassan", "Abdel Rahman", "Farouk", "Mansour", "El-Shazly", "Kamel", "Fathy",
            "El-Masry", "Nour El-Din", "Salem", "Zaki", "El-Gendy", "Younes", "Sabry", "Rashad",
            "El-Feky", "Adly", "Barakat", "Hegazy", "El-Tayeb", "Shaker", "Amer", "El-Naggar", "Farid"
        };

        public static readonly string[] Cities =
        {
            "Cairo", "Giza", "Alexandria", "Mansoura", "Tanta", "Zagazig", "Ismailia", "Aswan", "Luxor", "Damietta"
        };

        public static string FullName(bool isMale) =>
            $"{(isMale ? BoyFirstNames[Rng.Next(BoyFirstNames.Length)] : GirlFirstNames[Rng.Next(GirlFirstNames.Length)])} {FamilyNames[Rng.Next(FamilyNames.Length)]}";

        public static DateTime RandomDateBetween(DateTime start, DateTime end)
        {
            var range = (end - start).TotalMinutes;
            if (range <= 0) return start;
            return start.AddMinutes(Rng.NextDouble() * range);
        }

        // Registration growth curve: bias towards more-recent dates (platform has been growing).
        public static DateTime RandomJoinDate(DateTime earliest, DateTime latest)
        {
            var t = Math.Pow(Rng.NextDouble(), 0.6); // skew towards 1 (recent)
            var range = (latest - earliest).TotalMinutes;
            return earliest.AddMinutes(t * range);
        }

        public static T PickRandom<T>(IReadOnlyList<T> list) => list[Rng.Next(list.Count)];

        // skew < 1 biases towards `end` (recent), skew > 1 biases towards `start` (past).
        public static DateTime RandomDateWeighted(DateTime start, DateTime end, double skew)
        {
            if (end <= start) return start;
            var u = Math.Pow(Rng.NextDouble(), skew);
            var range = (end - start).TotalMinutes;
            return start.AddMinutes(u * range);
        }

        // Deterministic per-child tier, stable for the lifetime of a single seeding pass
        // (string.GetHashCode() is randomized per-process but constant within one run,
        // which is all that's needed since every Demo* seeder runs once, in the same process).
        public static ActivityTier TierForChild(string childId)
        {
            var rnd = new Random(childId.GetHashCode());
            var roll = rnd.NextDouble();
            if (roll < 0.20) return ActivityTier.Beginner;
            if (roll < 0.50) return ActivityTier.Casual;
            if (roll < 0.80) return ActivityTier.Active;
            if (roll < 0.95) return ActivityTier.StarPerformer;
            return ActivityTier.Veteran;
        }

        public static string Placeholder(string text, string size = "256x256") =>
            $"https://placehold.co/{size}?text={Uri.EscapeDataString(text)}";

        public enum InstitutionSize { Small, Medium, Large }

        // Single source of truth for the 9 demo institutions - referenced by index across
        // DemoInstitutionSeeder/DemoClassSeeder/DemoSupervisorSeeder/DemoParentChildSeeder
        // so each seeder can independently re-derive institution sizing without a shared DB round trip.
        public static readonly (string Name, string City, InstitutionSize Size)[] InstitutionDefs =
        {
            ("Al-Fikr International School", "Cairo", InstitutionSize.Large),
            ("Cairo British Nursery & School", "Giza", InstitutionSize.Large),
            ("Nahda Modern Language School", "Alexandria", InstitutionSize.Medium),
            ("Al-Salam Language School", "Mansoura", InstitutionSize.Medium),
            ("Future Generation Academy", "Tanta", InstitutionSize.Medium),
            ("Green Valley International School", "Zagazig", InstitutionSize.Medium),
            ("Sunrise Kindergarten", "Ismailia", InstitutionSize.Small),
            ("Al-Andalus Language School", "Aswan", InstitutionSize.Small),
            ("Little Explorers Nursery", "Damietta", InstitutionSize.Small),
        };

        public static int ClassesFor(InstitutionSize size) => size switch
        {
            InstitutionSize.Large => 5,
            InstitutionSize.Medium => 4,
            _ => 2
        };

        public static int SupervisorsFor(InstitutionSize size) => size switch
        {
            InstitutionSize.Large => 6,
            InstitutionSize.Medium => 4,
            _ => 2
        };

        public static int ChildrenPerClassFor(InstitutionSize size) => size switch
        {
            InstitutionSize.Large => 10,
            InstitutionSize.Medium => 8,
            _ => 6
        };

        public static string InstitutionCode(int index) => $"SCH-DEMO-{index + 1:000}";
        public static string ClassCode(int instIndex, int classIndex) => $"CLS-{instIndex + 1:00}-{classIndex + 1:00}";
    }
}
