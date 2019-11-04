// Copyright (c) 2019 All rights reserved.
// Code should follow the .NET Standard Guidelines:
// https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Miniblog.Core.Extensions
{
    public static class CloudBlockBlobExtensions
    {
        public static async Task<string> DownloadTextSimpleAsync(this CloudBlockBlob blockBlob, CancellationToken cancelToken = default)
        {
            if (blockBlob == null)
            {
                return null;
            }

            return await blockBlob.DownloadTextAsync(Encoding.UTF8,
                    null,
                    null,
                    null,
                    null,
                    cancelToken)
                .ConfigureAwait(false);
        }
    }
}