using System;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Core
{
    public class Poller : IPoller
    {
        public IPollAssertions<T> Poll<T>(Func<Task<T>> pollFunction) => new PollAssertions<T>(pollFunction);

        public IPollAssertions<T> Poll<T>(Func<T> pollFunction) => new PollAssertions<T>(pollFunction);
    }
}
