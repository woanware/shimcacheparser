using System;

namespace woanware
{
    /// <summary>
    /// 
    /// </summary>
    public class Hit
    {
        #region Member Variables
        public DateTime LastModified { get; internal set; }
        public DateTime LastUpdate { get; internal set; }
        public string Path { get; internal set; }
        public UInt64 FileSize { get; internal set; }
        public string ProcessExecFlag { get; internal set; }
        public Global.CacheType Type { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        public Hit()
        {
            Path = string.Empty;
            ProcessExecFlag = string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheType"></param>
        /// <param name="lastModified"></param>
        /// <param name="lastUpdate"></param>
        /// <param name="path"></param>
        /// <param name="fileSize"></param>
        /// <param name="processExecFlag"></param>
        public Hit(Global.CacheType cacheType,
                   DateTime lastModified, 
                   DateTime lastUpdate, 
                   string path,
                   UInt64 fileSize, 
                   string processExecFlag)
        {
            Type = cacheType;
            LastModified = lastModified;
            LastUpdate = lastUpdate;
            Path = path;
            FileSize = fileSize;
            ProcessExecFlag = processExecFlag;
        }
        #endregion
    }
}
