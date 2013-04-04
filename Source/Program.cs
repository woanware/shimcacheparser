using System;
using System.Collections.Generic;
using System.Text;
using Registry;
using System.Reflection;
using CommandLine;
using System.IO;
using System.Linq;
using CsvHelper;

namespace woanware
{
    /// <summary>
    /// 
    /// </summary>
    class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                AssemblyName assemblyName = assembly.GetName();

                Console.WriteLine(Environment.NewLine + "shimcacheparser v" + assemblyName.Version.ToString(3) + Environment.NewLine);

                Options options = new Options();
                if (CommandLineParser.Default.ParseArguments(args, options) == false)
                {
                    return;
                }

                if (IsValidSortValue(options) == false)
                {
                    Console.WriteLine("Invalid sort parameter value (-s)");
                    return;
                }

                if (File.Exists(options.File) == false)
                {
                    Console.WriteLine("The registry file does not exist");
                    return;
                }

                Registry.Registry registry = new Registry.Registry(options.File);
                RegistryKey rootKey = registry.Root;
                foreach (RegistryKey subKey in rootKey.SubKeys())
                {
                    if (subKey.Name.IndexOf("ControlSet", StringComparison.InvariantCultureIgnoreCase) == -1)
                    {
                        continue;
                    }

                    try
                    {
                        List<Hit> hits = new List<Hit>();

                        RegistryKey regKeySessionManager = registry.Open(string.Format(@"{0}\Control\Session Manager", subKey.Name));
                        if (regKeySessionManager == null)
                        {
                            continue;
                        }

                        foreach (RegistryKey subKeySessionManager in regKeySessionManager.SubKeys())
                        {
                            //@"ControlSet001\Control\Session Manager\AppCompatibility\AppCompatCache"
                            //@"ControlSet001\Control\Session Manager\AppCompatCache\AppCompatCache"
                            if (subKeySessionManager.Name.IndexOf("AppCompatibility", StringComparison.InvariantCultureIgnoreCase) == -1 &
                                subKeySessionManager.Name.IndexOf("AppCompatCache", StringComparison.InvariantCultureIgnoreCase) == -1)
                            {
                                continue;
                            }

                            RegistryValue regVal = subKeySessionManager.Value("AppCompatCache");
                            if (regVal == null)
                            {
                                continue;
                            }

                            byte[] data = (byte[])regVal.Value;

                            // Data size less than minimum header size.
                            if (data.Length < 16)
                            {
                                continue;
                            }

                            UInt32 magic = BitConverter.ToUInt32(data.Slice(0, 4), 0);

                            // Determine which version we are working with
                            switch (magic)
                            {
                                case Global.WINXP_MAGIC32: // This is WinXP cache data
                                    Console.WriteLine("[+] Found 32bit Windows XP Shim Cache data...");
                                    hits = ReadWinXpEntries(data);
                                    break;
                                case Global.CACHE_MAGIC_NT5_2:  // This is a Windows 2k3/Vista/2k8 Shim Cache format, 
                                    // Shim Cache types can come in 32-bit or 64-bit formats. We can determine this because 64-bit entries are serialized with u_int64 pointers.
                                    // This means that in a 64-bit entry, valid UNICODE_STRING sizes are followed by a NULL DWORD. Check for this here. 
                                    UInt16 testSizeNt5 = BitConverter.ToUInt16(data.Slice(8, 10), 0);
                                    UInt16 testMaxSizeNt5 = BitConverter.ToUInt16(data.Slice(10, 12), 0);
                                    UInt32 testTempNt5 = BitConverter.ToUInt32(data.Slice(12, 16), 0);

                                    if ((testMaxSizeNt5 - testSizeNt5 == 2) & (testTempNt5 == 0))
                                    {
                                        Console.WriteLine("[+] Found 64bit Windows 2k3/Vista/2k8 Shim Cache data...");
                                        hits = ReadNt5Entries(data, false);
                                    }
                                    else
                                    {
                                        Console.WriteLine("[+] Found 32bit Windows 2k3/Vista/2k8 Shim Cache data...");
                                        hits = ReadNt5Entries(data, true);
                                    }

                                    break;
                                case Global.CACHE_MAGIC_NT6_1: // This is a Windows 7/2k8-R2 Shim Cache.    
                                    // Shim Cache types can come in 32-bit or 64-bit formats. We can determine this because 64-bit entries are serialized with u_int64 pointers.
                                    // This means that in a 64-bit entry, valid UNICODE_STRING sizes are followed by a NULL DWORD. Check for this here. 
                                    UInt16 testSizeNt6 = BitConverter.ToUInt16(data.Slice(Global.CACHE_HEADER_SIZE_NT6_1, Global.CACHE_HEADER_SIZE_NT6_1 + 2), 0);
                                    UInt16 testMaxSizeNt6 = BitConverter.ToUInt16(data.Slice(Global.CACHE_HEADER_SIZE_NT6_1 + 2, Global.CACHE_HEADER_SIZE_NT6_1 + 4), 0);
                                    UInt32 testTempNt6 = BitConverter.ToUInt32(data.Slice(Global.CACHE_HEADER_SIZE_NT6_1 + 4, Global.CACHE_HEADER_SIZE_NT6_1 + 8), 0);

                                    if ((testMaxSizeNt6 - testSizeNt6 == 2) & (testTempNt6 == 0))
                                    {
                                        Console.WriteLine("[+] Found 64bit Windows 7/2k8-R2 Shim Cache data...");
                                        hits = ReadNt6Entries(data, false);
                                    }
                                    else
                                    {
                                        Console.WriteLine("[+] Found 32bit Windows 7/2k8-R2 Shim Cache data...");
                                        hits = ReadNt6Entries(data, true);
                                    }
                                    break;
                                default:
                                    Console.WriteLine(string.Format("[-] Got an unrecognized magic value of {0}... bailing... ", magic));
                                    return;
                            }
                        }

                        PrintHits(options, hits);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        /// <summary>
        /// Read the WinXP Shim Cache data. Some entries can be missing data but still contain useful information, so try to get as much as we can.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static List<Hit> ReadWinXpEntries(byte[] data)
        {
            List<Hit> hits = new List<Hit>();

            UInt32 numEntries = BitConverter.ToUInt32(data.Slice(8, 12), 0);
            if (numEntries == 0)
            {
                return hits;
            }

            for (UInt32 index = Global.WINXP_HEADER_SIZE32; index < (numEntries * Global.WINXP_ENTRY_SIZE32); index += Global.WINXP_ENTRY_SIZE32)
            {
                byte[] temp = data.Slice(index, (index + Global.WINXP_ENTRY_SIZE32));
                CacheEntryXp ce = new CacheEntryXp();
                ce.Update(temp);

                hits.Add(new Hit(Global.CacheType.CacheEntryXp, ce.ModDateTime, ce.ExecDateTime, ce.Path, ce.FileSize, "N/A"));
            }

            return hits;
        }

        /// <summary>
        /// Read Windows 2k3/Vista/2k8 Shim Cache entry formats.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="is32Bit"></param>
        /// <returns></returns>
        private static List<Hit> ReadNt5Entries(byte[] data, bool is32Bit)
        {
            List<Hit> hits = new List<Hit>();

            UInt32 entrySize = is32Bit == true ? Global.NT5_2_ENTRY_SIZE32 : Global.NT5_2_ENTRY_SIZE64;
            UInt32 numEntries = BitConverter.ToUInt32(data.Slice(4, 8), 0);

            bool containsFileSize = false;
            // On Windows Server 2008/Vista, the filesize is swapped out of this structure with two 4-byte flags.
            // Check to see if any of the values in "dwFileSizeLow" are larger than 2-bits. This indicates the entry contained file sizes.
            for (UInt32 index = Global.CACHE_HEADER_SIZE_NT5_2; index < (numEntries * entrySize); index += entrySize)
            {
                byte[] temp = data.Slice(index, (index + entrySize));
                CacheEntryNt5 ce = new CacheEntryNt5(is32Bit, containsFileSize);
                ce.Update(temp);

                if (ce.FileSizeLow > 3)
                {
                    containsFileSize = true;
                    break;
                }
            }

            // Now grab all the data in the value.
            for (UInt32 index = Global.CACHE_HEADER_SIZE_NT5_2; index < (numEntries * entrySize); index += entrySize)
            {
                byte[] temp = data.Slice(index, (index + entrySize));
                CacheEntryNt5 ce = new CacheEntryNt5(is32Bit, containsFileSize);
                ce.Update(temp);

                string path = Encoding.Unicode.GetString(data.Slice(ce.Offset, ce.Offset + ce.Length));//.decode('utf-16le','replace').encode('utf-8')
                path = path.Replace("\\??\\", string.Empty);

                // It contains file data.
                if (containsFileSize == true)
                {
                    hits.Add(new Hit(Global.CacheType.CacheEntryNt5, ce.DateTime, DateTime.MinValue, path, ce.FileSizeLow, "N/A"));
                }
                else
                {
                    hits.Add(new Hit(Global.CacheType.CacheEntryNt5, ce.DateTime, DateTime.MinValue, path, 0, ce.ProcessExec.ToString()));
                }
            }

            return hits;
        }

        /// <summary>
        /// Read the Shim Cache Windows 7/2k8-R2 entry format, return a list of last modifed dates/paths.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="is32Bit"></param>
        /// <returns></returns>
        private static List<Hit> ReadNt6Entries(byte[] data, bool is32Bit)
        {
            List<Hit> hits = new List<Hit>();

            UInt32 entrySize = is32Bit == true ? Global.NT6_1_ENTRY_SIZE32 : Global.NT6_1_ENTRY_SIZE64;
            UInt32 numEntries = BitConverter.ToUInt32(data.Slice(4, 8), 0);

            for (UInt32 index = Global.CACHE_HEADER_SIZE_NT6_1; index < (numEntries * entrySize); index += entrySize)
            {
                byte[] temp = data.Slice(index, (index + entrySize));
                CacheEntryNt6 ce = new CacheEntryNt6(is32Bit);
                ce.Update(temp);

                string path = Encoding.Unicode.GetString(data.Slice(ce.Offset, ce.Offset + ce.Length));
                path = path.Replace("\\??\\", string.Empty);

                hits.Add(new Hit(Global.CacheType.CacheEntryNt6, ce.DateTime, DateTime.MinValue, path, 0, ce.ProcessExec.ToString()));
            }

            return hits;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static CsvWriterOptions SetCsvWriterConfig(Options options)
        {
            CsvHelper.CsvWriterOptions csvWriterOptions = new CsvWriterOptions();
            try
            {
                switch (options.Delimiter)
                {
                    case "'\\t'":
                        csvWriterOptions.Delimiter = '\t';
                        break;
                    case "\\t":
                        csvWriterOptions.Delimiter = '\t';
                        break;
                    default:
                        csvWriterOptions.Delimiter = char.Parse(options.Delimiter);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to set delimiter. Defaulting to comma \",\": " + ex.Message);
                csvWriterOptions.Delimiter = ',';
            }

            return csvWriterOptions;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="hits"></param>
        /// <returns></returns>
        private static IEnumerable<Hit> GetSortedHits(Options options, List<Hit> hits)
        {
            switch (options.Sort.ToLower())
            {
                case "modified":
                    return from h in hits orderby h.LastModified select h;
                case "updated":
                    return from h in hits orderby h.LastUpdate select h;
                case "path":
                    return from h in hits orderby h.Path select h;
                case "filesize":
                    return from h in hits orderby h.FileSize select h;
                case "executed":
                    return from h in hits orderby h.ProcessExecFlag select h;
                default:
                    return from h in hits orderby h.LastUpdate select h;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="hits"></param>
        /// <returns></returns>
        private static bool IsValidSortValue(Options options)
        {
            switch (options.Sort.ToLower())
            {
                case "modified":
                    return true;
                case "updated":
                    return true;
                case "path":
                    return true;
                case "filesize":
                    return true;
                case "executed":
                    return true;
                case "":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="hits"></param>
        private static void PrintHits(Options options, List<Hit> hits)
        {
            try
            {
                CsvHelper.CsvWriterOptions csvWriterOptions = SetCsvWriterConfig(options);

                using (MemoryStream memoryStream = new MemoryStream())
                using (StreamWriter streamWriter = new StreamWriter(memoryStream))
                using (CsvHelper.CsvWriter csvWriter = new CsvHelper.CsvWriter(streamWriter, csvWriterOptions))
                {
                    // Write out the file headers
                    csvWriter.WriteField("Last Modified");
                    csvWriter.WriteField("Last Update");
                    csvWriter.WriteField("Path");
                    csvWriter.WriteField("File Size");
                    csvWriter.WriteField("Process Exec Flag");
                    csvWriter.NextRecord();

                    var sorted = GetSortedHits(options, hits);

                    foreach (Hit hit in sorted)
                    {
                        switch (hit.Type)
                        {
                            case Global.CacheType.CacheEntryXp: // Windows XP Shim Cache
                                csvWriter.WriteField(hit.LastModified.ToShortDateString() + " " + hit.LastModified.ToShortTimeString());
                                csvWriter.WriteField(hit.LastUpdate.ToShortDateString() + " " + hit.LastUpdate.ToShortTimeString());
                                csvWriter.WriteField(hit.Path);
                                csvWriter.WriteField(hit.FileSize.ToString());
                                csvWriter.WriteField(hit.ProcessExecFlag);
                                csvWriter.NextRecord();

                                break;
                            case Global.CacheType.CacheEntryNt5: // Windows 2k3/Vista/2k8 Shim Cache 
                            case Global.CacheType.CacheEntryNt6: // Windows 7/2k8-R2 Shim Cache 
                                csvWriter.WriteField(hit.LastModified.ToShortDateString() + " " + hit.LastModified.ToShortTimeString());
                                csvWriter.WriteField("N/A");
                                csvWriter.WriteField(hit.Path);
                                csvWriter.WriteField("N/A");
                                csvWriter.WriteField(hit.ProcessExecFlag);
                                csvWriter.NextRecord();
                                break;
                        }
                    }

                    string output = string.Empty;
                    memoryStream.Position = 0;
                    using (StreamReader streamReader = new StreamReader(memoryStream))
                    {
                        output = streamReader.ReadToEnd();
                    }

                    Console.Write(output);

                    if (options.Output.Length > 0)
                    {
                        string ret = IO.WriteUnicodeTextToFile(output, options.Output, false);
                        if (ret.Length > 0)
                        {

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }
    }
}