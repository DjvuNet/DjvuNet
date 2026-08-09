using System;
using System.Threading;
using System.Threading.Tasks;
using DjvuNet.Errors;
using DjvuNet.Tests;
using Xunit;
using Xunit.Sdk; // Contains ThrowsException for testing assertion failures

namespace DjvuNet.Shared.Tests
{

    public class ThrowsAsyncTests
    {
        private readonly Lock _testLock = new();
        private const int FastTimeout = 50;

        /// <summary>
        /// Spawns a dedicated thread to verify if the lock is actually released.
        /// Due to reentrant nature of Lock enter methods it has to be verified
        /// on separate thread.
        /// </summary>
        private void AssertLockIsFree()
        {
            bool cleanAcquisition = false;

            var verificationThread = new Thread(() =>
            {
                // TryEnter on a completely separate thread.
                // If the main thread leaked the lock, this will return false.
                cleanAcquisition = _testLock.TryEnter(TimeSpan.FromMilliseconds(10));
                if (cleanAcquisition)
                {
                    _testLock.Exit();
                }
            });

            verificationThread.Start();
            verificationThread.Join();

            Assert.True(cleanAcquisition, "Infrastructure leaked the lock! Main thread failed to release it.");
        }

        [Fact]
        public async Task ThrowsAsync_ExpectedException()
        {
            // Act
            var exception = await Util.ThrowsAsync<InvalidOperationException>(
                lockAcquisition: () => _testLock.Enter(),
                lockRelease: () => _testLock.Exit(),
                backgroundAction: () => throw new InvalidOperationException("Expected"),
                timeout: FastTimeout
            );

            // Assert
            Assert.NotNull(exception);
            Assert.Equal("Expected", exception.Message);

            AssertLockIsFree();
        }

        [Fact]
        public async Task ThrowsAsync_WrongException()
        {
            // Act & Assert
            // We expect DeadlockAssert to bubble up an xUnit ThrowsException 
            // because the background threw an ArgumentException instead of an InvalidOperationException
            await Assert.ThrowsAsync<ThrowsException>(() =>
                Util.ThrowsAsync<InvalidOperationException>(
                    lockAcquisition: () => _testLock.Enter(),
                    lockRelease: () => _testLock.Exit(),
                    backgroundAction: () => throw new ArgumentException("Wrong exception"),
                    timeout: FastTimeout
                )
            );

            AssertLockIsFree();
        }

        [Fact]
        public async Task ThrowsAsync_NoException()
        {
            // Act & Assert
            // If the background action succeeds without throwing, DeadlockAssert must fail the test
            await Assert.ThrowsAsync<ThrowsException>(() =>
                Util.ThrowsAsync<InvalidOperationException>(
                    lockAcquisition: () => _testLock.Enter(),
                    lockRelease: () => _testLock.Exit(),
                    backgroundAction: () => { /* Do nothing successfully */ },
                    timeout: FastTimeout
                )
            );

            AssertLockIsFree();
        }

        [Fact]
        public async Task ThrowsAsync_ThreadSleepTimeout()
        {
            // Act & Assert
            // Simulate a broken production code path where the background thread hangs.
            // ThrowsAsync should hit its fast timeout safety net and throw a TimeoutException.
            var exception = await Assert.ThrowsAsync<DjvuTimeoutException>(() =>
                Util.ThrowsAsync<InvalidOperationException>(
                    lockAcquisition: () => _testLock.Enter(),
                    lockRelease: () => _testLock.Exit(),
                    backgroundAction: () => { Thread.Sleep(5000); }, // Hangs past the 50ms limit
                    timeout: FastTimeout
                )
            );

            Assert.Contains("waiting for the background thread to finish", exception.Message);

            AssertLockIsFree();
        }

        [Fact]
        public async Task ThrowsAsync_ThreadSpinTimeout()
        {
            // Act & Assert
            // Simulate a broken production code path where the background thread enters a CPU spin.
            // Thread.Interrupt() will fail to stop it. ThrowsAsync MUST abandon it after Join(500) times out.
            var exception = await Assert.ThrowsAsync<DjvuTimeoutException>(() =>
                Util.ThrowsAsync<InvalidOperationException>(
                    lockAcquisition: () => _testLock.Enter(),
                    lockRelease: () => _testLock.Exit(),
                    backgroundAction: () => 
                    { 
                        var spinUntil = DateTime.UtcNow.AddSeconds(2);
                        while (DateTime.UtcNow < spinUntil) { /* Spin */ } 
                    },
                    timeout: FastTimeout
                )
            );

            Assert.Contains("waiting for the background thread to finish", exception.Message);

            AssertLockIsFree();
        }
    }
}
