using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace woanware
{
    /// <summary>
    /// Shim Cache format used by Windows 6.1 (Win7 through Server 2008 R2)
    /// </summary>
    internal class CacheEntryNt6
    {
        #region Member Variables
        public bool Is32Bit { get; private set; }
        public UInt16 Length { get; private set; }
        public UInt16 MaxLength { get; private set; }
        public UInt64 Offset { get; private set; }
        public DateTime DateTime { get; private set; }
        public UInt64 FileFlags { get; private set; }
        public UInt64 Flags { get; private set; }
        public UInt64 BlobSize { get; private set; }
        public UInt64 BlobOffset { get; private set; }
        public bool ProcessExec { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        /// <param name="is32Bit"></param>
        public CacheEntryNt6(bool is32Bit)
        {
            Is32Bit = is32Bit;
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

                if (Is32Bit == true)
                {
                    Length = StreamReaderHelper.ReadUInt16(memoryStream);
                    MaxLength = StreamReaderHelper.ReadUInt16(memoryStream);
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
                    
                    FileFlags = StreamReaderHelper.ReadUInt32(memoryStream);
                    Flags = StreamReaderHelper.ReadUInt32(memoryStream);
                    BlobSize = StreamReaderHelper.ReadUInt32(memoryStream);
                    BlobOffset = StreamReaderHelper.ReadUInt32(memoryStream); 
                }
                else
                {
                    Length = StreamReaderHelper.ReadUInt16(memoryStream);
                    MaxLength = StreamReaderHelper.ReadUInt16(memoryStream);

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

                    FileFlags = StreamReaderHelper.ReadUInt32(memoryStream);
                    Flags = StreamReaderHelper.ReadUInt32(memoryStream);
                    BlobSize = StreamReaderHelper.ReadUInt64(memoryStream);
                    BlobOffset = StreamReaderHelper.ReadUInt64(memoryStream); 
                }
            }

            // Test to see if the file may have been executed.
            if ((FileFlags & Global.CSRSS_FLAG) == Global.CSRSS_FLAG)
            {
                ProcessExec = true;
            }
            else
            {
                ProcessExec = false;
            }
        }
        #endregion

        ///// <summary>
        ///// 
        ///// </summary>
        //public UInt32 Size
        //{
        //    get
        //    {
        //        if (Is32Bit == true)
        //        {
        //            return Global.NT6_1_ENTRY_SIZE32;
        //        }

        //        return Global.NT6_1_ENTRY_SIZE64;
        //    }
        //}
    }
}
