﻿using CommunityToolkit.Diagnostics;
using OwlCore.Remoting;
using OwlCore.Remoting.Transfer;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Tests.Remoting.Transfer
{
    /// <summary>
    /// A simple implementation of <see cref="IRemoteMessageHandler"/> that routes messages sent with <see cref="SendMessageAsync(IRemoteMessage, CancellationToken?)"/> via <see cref="MessageReceived"/>.
    /// </summary>
    public class LoopbackMockMessageHandler : IRemoteMessageHandler
    {
        private SemaphoreSlim _sendMessageSemaphore = new SemaphoreSlim(1, 1);

        public LoopbackMockMessageHandler(RemotingMode mode)
        {
            Mode = mode;
        }

        /// <summary>
        /// A second instance to loop sent messages into. May be in a different <see cref="Mode"/>.
        /// </summary>
        public List<LoopbackMockMessageHandler> LoopbackListeners { get; } = new List<LoopbackMockMessageHandler>();

        /// <inheritdoc/>
        public RemotingMode Mode { get; set; }

        /// <inheritdoc/>
        public IRemoteMessageConverter? MessageConverter => null;

        /// <inheritdoc/>
        public bool IsInitialized { get; set; }

        /// <inheritdoc/>
        public MemberSignatureScope MemberSignatureScope { get; set; } = MemberSignatureScope.AssemblyQualifiedName;

        /// <inheritdoc/>
        public event EventHandler<IRemoteMessage>? MessageReceived;

        public void ReceiveMessage(IRemoteMessage memberMessage)
        {
            MessageReceived?.Invoke(this, memberMessage);
        }

        /// <inheritdoc/>
        public Task InitAsync(CancellationToken cancellationToken = default)
        {
            IsInitialized = true;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task SendMessageAsync(IRemoteMessage memberMessage, CancellationToken? cancellationToken = null)
        {
            using (await OwlCore.Flow.EasySemaphore(_sendMessageSemaphore))
            {
                Guard.IsNotNull(LoopbackListeners, nameof(LoopbackListeners));
                Guard.HasSizeGreaterThan(LoopbackListeners, 0, nameof(LoopbackListeners));

                foreach (var listener in LoopbackListeners)
                {
                    await Task.Run(async () =>
                    {
                        await Task.Delay(50); // Simulated network latency.
                        listener.ReceiveMessage(memberMessage);
                    });
                }
            }
        }
    }
}
