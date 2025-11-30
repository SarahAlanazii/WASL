

namespace Wasl.Infrastructure
{
    public static class AppConstants
    {
        // ============================================================
        // User Roles
        // ============================================================
        public const string ROLE_ADMIN = "Admin";
        public const string ROLE_COMPANY = "Company";
        public const string ROLE_PROVIDER = "Provider";

        // ============================================================
        // Admin Status
        // ============================================================
        public const string ADMIN_ACTIVE = "Active";
        public const string ADMIN_INACTIVE = "Inactive";
        public const string ADMIN_SUSPENDED = "Suspended";

        // ============================================================
        // Company Status
        // ============================================================
        public const string COMPANY_PENDING_APPROVAL = "PendingApproval";
        public const string COMPANY_APPROVED = "Approved";
        public const string COMPANY_REJECTED = "Rejected";
        public const string COMPANY_SUSPENDED = "Suspended";

        // ============================================================
        // Provider Status
        // ============================================================
        public const string PROVIDER_PENDING_APPROVAL = "PendingApproval";
        public const string PROVIDER_APPROVED = "Approved";
        public const string PROVIDER_REJECTED = "Rejected";
        public const string PROVIDER_SUSPENDED = "Suspended";

        // ============================================================
        // Shipment Request Status
        // ============================================================
        public const string SHIPMENT_PENDING = "Pending";
        public const string SHIPMENT_BIDDING = "Bidding";
        public const string SHIPMENT_ASSIGNED = "Assigned";
        public const string SHIPMENT_ACCEPTED = "Accepted";
        public const string SHIPMENT_IN_PROGRESS = "InProgress";
        public const string SHIPMENT_DELIVERED = "Delivered";
        public const string SHIPMENT_CANCELLED = "Cancelled";
        public const string SHIPMENT_FAILED = "Failed";
        public const string SHIPMENT_DIRECT_REQUEST = "DirectRequest";

        // ============================================================
        // Bid Status
        // ============================================================
        public const string BID_SUBMITTED = "Submitted";
        public const string BID_UNDER_REVIEW = "UnderReview";
        public const string BID_ACCEPTED = "Accepted";
        public const string BID_REJECTED = "Rejected";
        public const string BID_CANCELLED = "Cancelled";
        public const string BID_CONTRACT_CREATED = "ContractCreated";

        // ============================================================
        // Payment Status
        // ============================================================
        public const string PAYMENT_PENDING = "Pending";
        public const string PAYMENT_PROCESSING = "Processing";
        public const string PAYMENT_SUCCESSFUL = "Successful";
        public const string PAYMENT_FAILED = "Failed";
        public const string PAYMENT_REFUNDED = "Refunded";

        // ============================================================
        // Shipment Tracking Status
        // ============================================================
        public const string SHIPMENT_RECEIVED = "Received";
        public const string SHIPMENT_IN_TRANSIT = "InTransit";
        public const string SHIPMENT_DELIVERED_OK = "Delivered";
    }

    /// <summary>
    /// Saudi Arabia regions and cities
    /// </summary>
    public static class KSALocations
    {
        public static readonly Dictionary<string, string> Regions = new()
        {
            { "riyadh", "Riyadh Region" },
            { "makkah", "Makkah Region" },
            { "eastern", "Eastern Province" },
            { "madinah", "Madinah Region" },
            { "qassim", "Qassim Region" },
            { "hail", "Hail Region" },
            { "tabuk", "Tabuk Region" },
            { "northern_borders", "Northern Borders Region" },
            { "jazan", "Jazan Region" },
            { "najran", "Najran Region" },
            { "al_bahah", "Al Bahah Region" },
            { "al_jawf", "Al Jawf Region" },
            { "asir", "Asir Region" }
        };

        public static readonly Dictionary<string, Dictionary<string, string>> CitiesByRegion = new()
        {
            {
                "riyadh", new Dictionary<string, string>
                {
                    { "riyadh", "Riyadh" }, { "diriyah", "Diriyah" }, { "alkharj", "Al Kharj" },
                    { "dhurma", "Dhurma" }, { "muzahmiyya", "Al Muzahmiyya" },
                    { "wadi_ad_dawasir", "Wadi Ad-Dawasir" }, { "afif", "Afif" },
                    { "shagra", "Al Shagra" }, { "hotat_bani_tamim", "Hotat Bani Tamim" },
                    { "layla", "Layla" }, { "sulayyil", "As Sulayyil" }, { "aflaj", "Aflaj" },
                    { "dilam", "Dilam" }, { "ramah", "Ramah" }, { "thadiq", "Thadiq" },
                    { "huraymila", "Huraymila" }, { "majmaah", "Al Majmaah" },
                    { "quwayiyah", "Quwayiyah" }, { "dawadmi", "Ad Dawadmi" },
                    { "harmah", "Harmah" }, { "shaqra", "Shaqra" }, { "gat", "Al Gat" }
                }
            },
            {
                "makkah", new Dictionary<string, string>
                {
                    { "makkah", "Makkah" }, { "jeddah", "Jeddah" }, { "taif", "At Taif" },
                    { "kamil", "Al Kamil" }, { "khulays", "Khulays" }, { "qunfudhah", "Al Qunfudhah" },
                    { "lith", "Al Lith" }, { "rabigh", "Rabigh" }, { "ranyah", "Ranyah" },
                    { "turbah", "Turbah" }, { "khurmah", "Al Khurmah" }, { "maysan", "Maysan" },
                    { "adhlam", "Adhlam" }, { "jammah", "Al Jammah" }
                }
            },
            {
                "eastern", new Dictionary<string, string>
                {
                    { "dammam", "Dammam" }, { "khobar", "Al Khobar" }, { "dhahran", "Dhahran" },
                    { "jubail", "Al Jubail" }, { "qatif", "Al Qatif" }, { "hafr_albatin", "Hafr Al-Batin" },
                    { "ahsa", "Al Ahsa" }, { "jubail_industrial", "Jubail Industrial City" },
                    { "ras_tanura", "Ras Tanura" }, { "abqaiq", "Abqaiq" },
                    { "nuayriyah", "An Nuayriyah" }, { "qaryat_alulaya", "Qaryat Al Ulaya" }
                }
            },
            {
                "madinah", new Dictionary<string, string>
                {
                    { "madinah", "Al Madinah" }, { "yanbu", "Yanbu" }, { "badr", "Badr" },
                    { "khaybar", "Khaybar" }, { "al_ula", "Al Ula" },
                    { "mahd_adh_dhahab", "Mahd Adh Dhahab" }, { "hanakiyah", "Al Hanakiyah" },
                    { "wajh", "Al Wajh" }, { "mabouk", "Mabouk" }
                }
            },
            {
                "qassim", new Dictionary<string, string>
                {
                    { "buraidah", "Buraidah" }, { "unayzah", "Unaizah" }, { "rass", "Ar Rass" },
                    { "muthnab", "Al Muthnab" }, { "bukayriyah", "Al Bukayriyah" },
                    { "badaya", "Al Badaya" }, { "riyadh_alkhabra", "Riyadh Al Khabra" },
                    { "nabhaniyah", "An Nabhaniyah" }, { "ash_shimasiyah", "Ash Shimasiyah" },
                    { "dariyah", "Ad Dariyah" }
                }
            },
            {
                "hail", new Dictionary<string, string>
                {
                    { "hail", "Hail" }, { "baqa", "Al Baqa" }, { "shinan", "Ash Shinan" },
                    { "ghazala", "Al Ghazala" }, { "sumaira", "As Sumaira" },
                    { "mawqaq", "Mawqaq" }, { "kutum", "Al Kutum" }
                }
            },
            {
                "tabuk", new Dictionary<string, string>
                {
                    { "tabuk", "Tabuk" }, { "duba", "Duba" }, { "alwajh", "Al Wajh" },
                    { "haql", "Haql" }, { "umluj", "Umluj" }, { "tayma", "Tayma" },
                    { "sharma", "Ash Sharma" }
                }
            },
            {
                "northern_borders", new Dictionary<string, string>
                {
                    { "arar", "Arar" }, { "turaif", "Turaif" }, { "rafha", "Rafha" },
                    { "al_uwqaylah", "Al Uwqaylah" }
                }
            },
            {
                "jazan", new Dictionary<string, string>
                {
                    { "jazan", "Jazan" }, { "sabya", "Sabya" }, { "abu_arish", "Abu Arish" },
                    { "damad", "Damad" }, { "samtah", "Samtah" }, { "al_ardah", "Al Ardah" },
                    { "baish", "Baish" }, { "farasan", "Farasan" }, { "dayer", "Ad Dayer" },
                    { "aidabi", "Aidabi" }, { "al_harth", "Al Harth" }
                }
            },
            {
                "najran", new Dictionary<string, string>
                {
                    { "najran", "Najran" }, { "sharourah", "Sharourah" }, { "hubuna", "Hubuna" },
                    { "badr_aljanub", "Badr Al Janub" }, { "yadamah", "Yadamah" },
                    { "thar", "Thar" }, { "khabash", "Khabash" }
                }
            },
            {
                "al_bahah", new Dictionary<string, string>
                {
                    { "al_bahah", "Al Bahah" }, { "baljurashi", "Baljurashi" },
                    { "al_mandaq", "Al Mandaq" }, { "al_makhwah", "Al Makhwah" },
                    { "al_aqiq", "Al Aqiq" }, { "qilwah", "Qilwah" }, { "mandaq", "Mandaq" }
                }
            },
            {
                "al_jawf", new Dictionary<string, string>
                {
                    { "sakaka", "Sakaka" }, { "qurayyat", "Qurayyat" },
                    { "dawmat_aljandal", "Dawmat Al Jandal" }, { "tabarjal", "Tabarjal" },
                    { "al_isawiyah", "Al Isawiyah" }
                }
            },
            {
                "asir", new Dictionary<string, string>
                {
                    { "abha", "Abha" }, { "khamis_mushait", "Khamis Mushait" },
                    { "bisha", "Bisha" }, { "muhayil", "Muhayil" },
                    { "sarat_abidah", "Sarat Abidah" }, { "nanman", "An Naman" },
                    { "dhahran_aljanub", "Dhahran Al Janub" }, { "tathlith", "Tathlith" },
                    { "rijal_almah", "Rijal Alma" }, { "balqarn", "Balqarn" },
                    { "majardah", "Majardah" }
                }
            }
        };
    }
}