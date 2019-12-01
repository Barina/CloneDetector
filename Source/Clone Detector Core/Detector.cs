using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace CloneDetectorCore
{
    public class Detector
    {
        /// <summary>
        /// Gets the MD5 checksum of a file as a <see cref="string"/>.
        /// </summary>
        /// <param name="filename">The path to the file to generate MD5 for.</param>
        /// <returns>Returns a new <see cref="string"/> containing the calculated 
        /// MD5 checksum for the given file or an empty string if cannot calculate 
        /// or the file doesn't exists.</returns>
        public static string GetMD5(string filename) =>
            BitConverter.ToString(GetMD5AsByteArray(filename)).Replace("-", "").ToLower();

        /// <summary>
        /// Calculate the MD5 checksum of a given file and return it as an array of <see cref="byte"/>s.
        /// </summary>
        /// <param name="filename">The path to the file to generate MD5 for.</param>
        /// <returns>Returns array of <see cref="byte"/>s containing the calculated 
        /// MD5 checksum for the given file or an empty array if cannot calculate 
        /// or the file doesn't exists.</returns>
        public static byte[] GetMD5AsByteArray(string filename)
        {
            var result = new byte[] { };
            if (File.Exists(filename))
                using (var stream = File.OpenRead(filename))
                using (var md5 = MD5.Create())
                    result = md5.ComputeHash(stream);
            return result;
        }

        /// <summary>
        /// Gets the size of a given file.
        /// </summary>
        /// <param name="filename">The path to the file.</param>
        /// <returns>Returns the size of the given file. -1 if not exists.</returns>
        public static long GetFileSize(string filename)
        {
            if (!File.Exists(filename)) return -1;
            return new FileInfo(filename).Length;
        }

        /// <summary>
        /// Gets a <see cref="Dictionary{TKey, TValue}"/> containing duplicate files calculated from a given file path array.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to test whether the user wants to cancel the operation.</param>
        /// <param name="progress">A <see cref="IProgress{T}"/> to update the caller about the on going state of the operation.</param>
        /// <param name="filePaths">The <see cref="string"/> array containing all the files to test. Must includes at least 2 files
        /// for this operation to start.</param>
        /// <returns>Returns a new <see cref="Dictionary{TKey, TValue}"/> with all the duplicates found pack with each file's MD5 checksum.</returns>
        public static async Task<Dictionary<string, List<string>>> GetClonedAsync(
            CancellationToken cancellationToken, IProgress<double> progress, params string[] filePaths) =>
            await Task.Run(() =>
            {
                if (filePaths == null || filePaths.Length <= 0) return default;

                // remove duplicates file path
                var distinctPaths = filePaths.Distinct().ToList();

                // initiate values
                var md5List = new Dictionary<string, string>();
                var result = new Dictionary<string, List<string>>();

                // calculating the overall amount of tests
                // using the formula for sum of arithmetic progression mentioned here:
                // https://he.wikipedia.org/wiki/%D7%A1%D7%93%D7%A8%D7%94_%D7%97%D7%A9%D7%91%D7%95%D7%A0%D7%99%D7%AA
                int testCount = distinctPaths.Count * (2 * 1 + (distinctPaths.Count - 1) * 1) / 2;

                int filesTested = 0;
                string filePathMD5;
                string otherFilePathMD5;
                for (int i = 0; i < distinctPaths.Count - 1; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return result;

                    // get the first file to test and generate it's MD5 if not already generated
                    string filePath = distinctPaths[i];
                    if (!md5List.ContainsKey(filePath))
                        md5List[filePath] = GetMD5(filePath);
                    filePathMD5 = md5List[filePath];

                    // check if this file has already been compared and reported as a duplicate
                    // more info: if a MD5 exists in the result list it means this file has
                    // already been compared with the rest of the files and all of the duplicates
                    // added to the result list as well.. so we need to ignore them to prevent 
                    // redundant duplicates in our result list
                    bool filePathAlreadyCompared = result.ContainsKey(filePathMD5);

                    // begin to loop over the files..
                    for (int j = i + 1; j < distinctPaths.Count; j++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return result;

                        // only test if there's actual value to test..
                        if (!string.IsNullOrEmpty(filePathMD5))
                        {
                            // get the other file path
                            string otherFilePath = distinctPaths[j];

                            // get or generate the other file MD5
                            if (!md5List.ContainsKey(otherFilePath))
                                md5List[otherFilePath] = GetMD5(otherFilePath);
                            otherFilePathMD5 = md5List[otherFilePath];

                            // the first iteration will actually calculate the MD5 for each file
                            // which takes more time and processing power..
                            // after that the calculation part is over and we only comparing string values
                            // therefore the reporting progress becomes heavy and may stuck the UI thread..

                            // compare the two files
                            if (!filePathAlreadyCompared &&
                                !string.IsNullOrEmpty(otherFilePathMD5) &&
                                filePathMD5 == otherFilePathMD5)
                            {
                                if (!result.ContainsKey(filePathMD5))
                                    result[filePathMD5] = new List<string> { filePath };
                                result[filePathMD5].Add(otherFilePath);
                            }
                        }

                        filesTested++;
                        // making sure the first double is a double, the second double is also a double
                        // and the calculation of them also returns a double and casting it implicitly to double!
                        progress?.Report((double)((double)filesTested / (double)testCount));
                    }
                }
                return result;
            });

        /// <summary>
        /// Gets a <see cref="Dictionary{TKey, TValue}"/> containing duplicate files calculated from a given file path array. 
        /// This is the detailed version where every step is reported at the progress.
        /// Could be less performant, consider using the regular version instead.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to test whether the user wants to cancel the operation.</param>
        /// <param name="progress">A <see cref="IProgress{T}"/> to update the caller about the on going state of the operation.</param>
        /// <param name="filePaths">The <see cref="string"/> array containing all the files to test. Must includes at least 2 files
        /// for this operation to start.</param>
        /// <returns>Returns a new <see cref="Dictionary{TKey, TValue}"/> with all the duplicates found pack with each file's MD5 checksum.</returns>
        public static async Task<Dictionary<string, List<string>>> GetClonedDetailedAsync(
            CancellationToken cancellationToken, IProgress<DetectionProgress> progress, params string[] filePaths) =>
            await Task.Run(() =>
            {
                if (filePaths == null || filePaths.Length <= 0) return default;

                // remove duplicates file path
                var distinctPaths = filePaths.Distinct().ToList();

                // initiate values
                var md5List = new Dictionary<string, string>();
                var result = new Dictionary<string, List<string>>();
                var progressItem = new DetectionProgress
                {
                    TestingFileCount = distinctPaths.Count,
                    FirstFilePath = "Gathering information.",
                    // calculating the test amount here to prevent redundant calculations after already calculated...
                    TestCount = distinctPaths.Count * (2 * 1 + (distinctPaths.Count - 1) * 1) / 2
                };

                progress?.Report(progressItem);

                string filePathMD5;
                string otherFilePathMD5;
                for (int i = 0; i < distinctPaths.Count - 1; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return result;

                    // get the first file to test and generate it's MD5 if not already generated
                    string filePath = distinctPaths[i];
                    progressItem.SubTestingFilesCount = distinctPaths.Count - i - 1;
                    progressItem.FirstFilePath = filePath;

                    if (!md5List.ContainsKey(filePath))
                        md5List[filePath] = GetMD5(filePath);
                    filePathMD5 = md5List[filePath];

                    // check if this file has already been compared and reported as a duplicate
                    // more info: if a MD5 exists in the result list it means this file has
                    // already been compared with the rest of the files and all of the duplicates
                    // added to the result list as well.. so we need to ignore them to prevent 
                    // redundant duplicates in our result list
                    bool filePathAlreadyCompared = result.ContainsKey(filePathMD5);

                    // begin to loop over the files..
                    for (int j = i + 1; j < distinctPaths.Count; j++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return result;

                        // only test if there's actual value to test..
                        if (!string.IsNullOrEmpty(filePathMD5))
                        {
                            // get the other file path
                            string otherFilePath = distinctPaths[j];
                            // calculate the sub progress
                            progressItem.SubProgress = ((double)j - (double)i) / (double)progressItem.SubTestingFilesCount;

                            // get or generate the other file MD5
                            if (!md5List.ContainsKey(otherFilePath))
                                md5List[otherFilePath] = GetMD5(otherFilePath);
                            otherFilePathMD5 = md5List[otherFilePath];

                            // the first iteration will actually calculate the MD5 for each file
                            // which takes more time and processing power..
                            // after that the calculation part is over and we only comparing string values
                            // therefore the reporting progress becomes heavy and may stuck the UI thread..
                            // we can filter some of the report to prevent the caller from non-responding UI
                            if (i < 1 || j % (distinctPaths.Count * .5) == 0)
                            {
                                progressItem.OtherFilePath = otherFilePath;
                                progress?.Report(progressItem);
                            }

                            // compare the two files
                            if (!filePathAlreadyCompared &&
                                !string.IsNullOrEmpty(otherFilePathMD5) &&
                                filePathMD5 == otherFilePathMD5)
                            {
                                if (!result.ContainsKey(filePathMD5))
                                    result[filePathMD5] = new List<string> { filePath };
                                result[filePathMD5].Add(otherFilePath);
                                progressItem.ClonesCount++;
                            }
                        }

                        progressItem.FilesTested++;
                    }
                }
                progress?.Report(progressItem);
                return result;
            });

        public struct DetectionProgress
        {
            /// <summary>
            /// Gets the overall progress of the detection process.
            /// </summary>
            public double OverallProgress =>
                (double)FilesTested / (double)TestCount;

            /// <summary>
            /// Gets the progress for the current file.
            /// </summary>
            public double SubProgress { get; internal set; }

            /// <summary>
            /// Gets the amount of clones detected so far.
            /// </summary>
            public int ClonesCount { get; internal set; }

            /// <summary>
            /// Gets the amount of tests the detector will perform.
            /// </summary>
            public int TestCount { get; internal set; }

            /// <summary>
            /// Gets the amount of files tested so far.
            /// </summary>
            public int FilesTested { get; internal set; }

            /// <summary>
            /// Gets the amount of files to test.
            /// </summary>
            public int TestingFileCount { get; internal set; }

            /// <summary>
            /// Gets the amount of files to compare against the first file.
            /// </summary>
            public int SubTestingFilesCount { get; internal set; }

            /// <summary>
            /// The path to the first file.
            /// </summary>
            public string FirstFilePath { get; internal set; }

            /// <summary>
            /// The path to the current file tested against the first file path.
            /// </summary>
            public string OtherFilePath { get; internal set; }
        }
    }
}
