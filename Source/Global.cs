namespace woanware
{
    /// <summary>
    /// Global class to store enums, constants etc
    /// </summary>
    public class Global
    {
        /// <summary>
        /// 
        /// </summary>
        public enum CacheType
        {
            CacheEntryXp = 0,
            CacheEntryNt5 = 1,
            CacheEntryNt6 = 2
        }

        // Values used by Windows 5.2 and 6.0 (Server 2003 through Vista/Server 2008)
        public const uint CACHE_MAGIC_NT5_2 = 0xbadc0ffe;
        public const uint CACHE_HEADER_SIZE_NT5_2 = 0x8;
        public const uint NT5_2_ENTRY_SIZE32 = 0x18;
        public const uint NT5_2_ENTRY_SIZE64 = 0x20;

        // Values used by Windows 6.1 (Win7 through Server 2008 R2)
        public const uint CACHE_MAGIC_NT6_1 = 0xbadc0fee;
        public const uint CACHE_HEADER_SIZE_NT6_1 = 0x80;
        public const uint NT6_1_ENTRY_SIZE32 = 0x20;
        public const uint NT6_1_ENTRY_SIZE64 = 0x30;
        public const uint CSRSS_FLAG = 0x2;

        // Values used by Windows 5.1 (WinXP 32-bit)
        public const uint WINXP_MAGIC32 = 0xdeadbeef;
        public const uint WINXP_HEADER_SIZE32 = 0x190;
        public const uint WINXP_ENTRY_SIZE32 = 0x228;
        public const uint MAX_PATH = 520;
    }
}
