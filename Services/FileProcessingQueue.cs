using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Zenko.Services
{
    public class FileProcessingQueue
    {
        private readonly Channel<QueuedFile> _channel = Channel.CreateUnbounded<QueuedFile>();

        public ValueTask QueueAsync(QueuedFile file) => _channel.Writer.WriteAsync(file);

        public IAsyncEnumerable<QueuedFile> DequeueAsync(System.Threading.CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public class QueuedFile
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public byte[] Content { get; set; }
    }
}
