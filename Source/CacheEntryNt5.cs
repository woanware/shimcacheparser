using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace woanware
{
    /// <summary>
    /// Shim Cache format used by Windows 5.2 and 6.0 (Server 2003 through Vista/Server 2008)
    /// </summary>
    internal class CacheEntryNt5
    {
        #region Member Variables
        private bool _containsFileSize;
        public bool Is32Bit { get; private set; }
        public string Path { get; private set; }
        public UInt16 Length { get; private set; }
        public UInt16 MaxLength { get; private set; }
        public UInt64 Offset { get; private set; }
        public DateTime DateTime { get; private set; }
        public UInt64 FileSizeLow { get; private set; }
        public UInt64 FileSizeHigh { get; private set; }
        public UInt32 FileSize { get; private set; }
        public bool ProcessExec { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        /// <param name="is32Bit"></param>
        /// <param name="containsFileSize"></param>
        public CacheEntryNt5(bool is32Bit, 
                             bool containsFileSize)
        {
            Is32Bit = is32Bit;
            _containsFileSize = containsFileSize;
        }
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

                Length = StreamReaderHelper.ReadUInt16(memoryStream);
                MaxLength = StreamReaderHelper.ReadUInt16(memoryStream);

                if (Is32Bit == true)
                {
                    Offset = StreamReaderHelper.ReadUInt32(memoryStream);

                    try
                    {
                        UInt32 lowDateTime = StreamReaderHelper.ReadUInt32(memoryStream);
                        UInt32 highDateTime = StreamReaderHelper.ReadUInt32(memoryStream);
                        long hFT2 = (((long)highDateTime) << 32) + lowDateTime;
                        DateTime = DateTime.FromFileTimeUtc(hFT2);
                    }
                    catch (Exception) 
                    {
                        DateTime = DateTime.MinValue;
                    }

                    FileSizeLow = StreamReaderHelper.ReadUInt32(memoryStream);
                    FileSizeHigh = StreamReaderHelper.ReadUInt32(memoryStream);
                }
                else
                {
                    memoryStream.Seek(4, SeekOrigin.Current);

                    Offset = StreamReaderHelper.ReadUInt64(memoryStream);

                    try
                    {
                        UInt32 lowDateTime = StreamReaderHelper.ReadUInt32(memoryStream);
                        UInt32 highDateTime = StreamReaderHelper.ReadUInt32(memoryStream);
                        long hFT2 = (((long)highDateTime) << 32) + lowDateTime;
                        DateTime = DateTime.FromFileTimeUtc(hFT2);
                    }
                    catch (Exception)
                    {
                        DateTime = DateTime.MinValue;
                    }

                    FileSizeLow = StreamReaderHelper.ReadUInt32(memoryStream);
                    FileSizeHigh = StreamReaderHelper.ReadUInt32(memoryStream);
                }
            }

            // It contains file data.
            if (_containsFileSize == false)
            {
                // Check the CSRSS flag.
                if ((FileSizeLow & Global.CSRSS_FLAG) == Global.CSRSS_FLAG)
                {
                    ProcessExec = true;
                }
                else
                {
                    ProcessExec = false;
                }
            }
        }
        #endregion
    }
}
