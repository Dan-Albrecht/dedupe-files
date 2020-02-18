namespace DedupeFiles
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;

    internal static class Exts
    {
        public static bool AtLeast<T>(this IEnumerable<T> stuff, int atLeast)
        {
            if (stuff == null)
            {
                return false;
            }

            if (atLeast == 0)
            {
                return true;
            }

            int count = 0;
            foreach (T t in stuff)
            {
                if (++count >= atLeast)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class Searcher
    {
        private readonly DirectoryInfo searchPath;

        public Searcher(DirectoryInfo searchPath)
        {
            this.searchPath = searchPath ?? throw new ArgumentNullException(nameof(searchPath));
        }

        private class ByteComp : IEqualityComparer<byte[]>
        {
            public static ByteComp Instance = new ByteComp();

            public bool Equals([AllowNull] byte[] x, [AllowNull] byte[] y)
            {
                if (x is null)
                {
                    if (y is null)
                    {
                        return true;
                    }

                    return false;
                }

                if (y is null)
                {
                    return false;
                }

                if (x.LongLength != y.LongLength)
                {
                    return false;
                }

                for (long i = 0; i < x.LongLength; i++)
                {
                    if (x[i] != y[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode([DisallowNull] byte[] obj)
            {
                // lol, whatever
                return obj.Length;
            }
        }

        internal void Search()
        {
            searchPath
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .ToLookup(item => item.Length) // Files of different lengths aren't the same
                .Where(item => item.AtLeast(2))
                .Select(item => item
                    .ToLookup(item2 => GetTheMiddleBytes(item2), ByteComp.Instance)
                    .Where(item2 => item2.AtLeast(2))
                    .Select(item2 => item2
                        .ToLookup(item3 => GetMD5(item3), ByteComp.Instance)
                        .Where(item3 => item3.AtLeast(2))
                        .Select(item3 => string.Join("; ", item3.Select(item4 => item4.FullName)) + Environment.NewLine)))
                .AsParallel()
                .SelectMany(item => item)
                .SelectMany(item => item)
                .ForAll(item => Console.WriteLine(item));
        }


        private byte[] GetMD5(FileInfo item)
        {
            using (var md5 = MD5.Create())
            using (var file = File.OpenRead(item.FullName))
            {
                byte[] result = md5.ComputeHash(file);
                return result;
            }
        }

        private byte[] GetTheMiddleBytes(FileInfo item)
        {
            try
            {
                const int length = 1024;
                if (item.Length <= length)
                {
                    return File.ReadAllBytes(item.FullName);
                }

                // Quick way to get bytes from around the middle of the file
                // Assumption is begining and ending might be fixed data (e.g. headers) and won't be sufficently unique
                int middleStart = (int)(item.Length / 2);
                middleStart -= length;

                if (middleStart < 0)
                {
                    middleStart = 0;
                }

                using (var file = File.OpenRead(item.FullName))
                {
                    file.Seek(middleStart, SeekOrigin.Begin);
                    byte[] theBytes = new byte[length];
                    int read = file.Read(theBytes, 0, length);
                    if (read != length)
                    {
                        throw new InvalidOperationException($"Something screwed up {read} != {length}");
                    }

                    return theBytes;
                }
            }
            catch (IOException ioException)
            {
                ioException.ToString();
                return null;
            }
        }
    }
}
