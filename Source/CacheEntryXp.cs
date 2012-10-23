using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace woanware
{
    /// <summary>
    /// Shim Cache format used by Windows XP
    /// </summary>
    internal class CacheEntryXp
    {
        #region Member Variables
        public string Path { get; private set; }
        public DateTime ModDateTime { get; private set; }
        public DateTime ExecDateTime { get; private set; }
        public UInt64 FileSize { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public CacheEntryXp() { }
        #endregion

        #region Methods
        /// <summary>
        /// Performs the parsing of the cache entry data
        /// </summary>
        public void Update(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                memoryStream.Seek(0, SeekOrigin.Begin);

                // No size size values are included in these entries, so search for utf-16 terminator.
                int[] ret = data.Slice(0, 0 + (Global.MAX_PATH + 8)).Locate(new byte[] { 00, 00 });

                if (ret.Length == 0)
                {
                    return;
                }

                string path = Encoding.Unicode.GetString(data.Slice(0, (UInt32)(0 + ret[0] + 1)));
                path = path.Replace("\\??\\", string.Empty);

                if (path.Trim().Length == 0)
                {
                    return;
                }

                Path = path;

                UInt32 entryOffset = 0 + Global.MAX_PATH + 8;
                memoryStream.Seek(entryOffset, SeekOrigin.Begin);

                try
                {
                    UInt32 lowDateTime = StreamReaderHelper.ReadUInt32(memoryStream);
                    UInt32 highDateTime = StreamReaderHelper.ReadUInt32(memoryStream);
                    long hFT2 = (((long)highDateTime) << 32) + lowDateTime;
                    ModDateTime = DateTime.FromFileTimeUtc(hFT2);
                }
                catch (Exception) 
                {
                    ModDateTime = DateTime.MinValue;
                }

                FileSize = StreamReaderHelper.ReadUInt64(memoryStream);
                if (FileSize == 0)
                {
                    return;
                }

                try
                {
                    UInt32 lowDateTime = StreamReaderHelper.ReadUInt32(memoryStream);
                    UInt32 highDateTime = StreamReaderHelper.ReadUInt32(memoryStream);
                    long hFT2 = (((long)highDateTime) << 32) + lowDateTime;
                    ExecDateTime = DateTime.FromFileTimeUtc(hFT2);
                }
                catch (Exception) 
                {
                    ExecDateTime = DateTime.MinValue;
                }
            }
        }
        #endregion
    }
}
