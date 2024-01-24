using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
    public class PollAssertions<T> : IPollAssertions<T>
    {
        private readonly Func<Task<T>> _pollFunction;
        private readonly int _defaultTimeout;

        public PollAssertions(Func<Task<T>> pollFunction)
        {
            _pollFunction = pollFunction ?? throw new ArgumentNullException(nameof(pollFunction));
            _defaultTimeout = 5_000;
        }

        public PollAssertions(Func<T> pollFunction)
        {
            if (pollFunction == null)
            {
                throw new ArgumentNullException(nameof(pollFunction));
            }
            _pollFunction = () => Task.Run(() => pollFunction());
            _defaultTimeout = 5_000;
        }

        public async Task ToBeAsync(T expected, PollAssertionsOptions options = null)
        {
            var matcherFunction = (T actual) => expected?.Equals(actual) ?? actual == null;

            await PollingLoopAsync(matcherFunction, options).ConfigureAwait(false);
        }

        private async Task PollingLoopAsync(Func<T, bool> matcherFunction, PollAssertionsOptions options = null)
        {
            var timeout = options?.Timeout.HasValue == true ? options.Timeout.Value : _defaultTimeout;
            var timeoutDeadline = DateTime.UtcNow.AddMilliseconds(timeout);

            if (timeout == 0)
            {
                await InnerLoop().ConfigureAwait(false);
            }
            else
            {
                // Setting the timeout here ensures no interval or poll function can exceed the "global" timeout
                await InnerLoop().WithTimeout(timeout).ConfigureAwait(false);
            }

            async Task InnerLoop()
            {
                var intervals = GetIntervalsOrDefault(options);
                var intervalCounter = 0;

                do
                {
                    var actual = await _pollFunction().ConfigureAwait(false);
                    if (matcherFunction(actual))
                    {
                        return;
                    }

                    await Task.Delay(intervals.ElementAt(intervalCounter)).ConfigureAwait(false);
                    intervalCounter = Math.Min(intervalCounter + 1, intervals.Count() - 1);
                }
                while (DateTime.UtcNow < timeoutDeadline || timeout == 0);
            }
        }

        private IEnumerable<int> GetIntervalsOrDefault(PollAssertionsOptions options)
        {
            if (options?.Intervals is null || !options.Intervals.Any())
            {
                return new[] { 100, 250, 500, 1000 };
            }
            return options?.Intervals;
        }
    }
}
