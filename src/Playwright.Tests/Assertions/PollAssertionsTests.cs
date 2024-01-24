using System.Diagnostics;

namespace Microsoft.Playwright.Tests.Assertions
{
    public class PollAssertionsTests : PageTestEx
    {
        [PlaywrightTest("playwright-test/expect-poll.spec.ts", "should poll predicate")]
        public async Task ShouldPollPredicate()
        {
            var counter = 0;

            await Expect().Poll(async () => { await Task.Delay(50); return ++counter; }).ToBeAsync(3, new() { Intervals = new[] { 50 }});

            await Expect().Poll(() => { return ++counter; }).ToBeAsync(6, new() { Intervals = new[] { 50 } });
            Assert.AreEqual(6, counter);
        }

        [PlaywrightTest()]
        public void ShouldUseDefaultIntervalsUntilTimeout()
        {
            var calledTimes = new List<TimeSpan>();
            var startTime = DateTime.UtcNow;

            Assert.ThrowsAsync(Is.TypeOf<TimeoutException>(),
                async () => await Expect().Poll(async () => { calledTimes.Add(DateTime.UtcNow - startTime); await Task.Delay(100); return 0; }).ToBeAsync(1));

            // interval timeout + default delay
            Assert.AreEqual(7, calledTimes.Count);
            Assert.AreEqual(0, calledTimes[0].TotalMilliseconds, 100); // immediate in first poll
            Assert.AreEqual(calledTimes[0].TotalMilliseconds + 100 + 100, calledTimes[1].TotalMilliseconds, 100);
            Assert.AreEqual(calledTimes[1].TotalMilliseconds + 250 + 100, calledTimes[2].TotalMilliseconds, 100);
            Assert.AreEqual(calledTimes[2].TotalMilliseconds + 500 + 100, calledTimes[3].TotalMilliseconds, 100); 
            Assert.AreEqual(calledTimes[3].TotalMilliseconds + 1000 + 100, calledTimes[4].TotalMilliseconds, 100);
            Assert.AreEqual(calledTimes[4].TotalMilliseconds + 1000 + 100, calledTimes[5].TotalMilliseconds, 100);
            Assert.AreEqual(calledTimes[5].TotalMilliseconds + 1000 + 100, calledTimes[6].TotalMilliseconds, 100);
        }

        [PlaywrightTest("playwright-test/expect-poll.spec.ts", "should respect interval")]
        public void ShouldUseCustomIntervalsUntilTimeout()
        {
            var calledTimes = new List<TimeSpan>();
            var startTime = DateTime.UtcNow;

            Assert.ThrowsAsync(Is.TypeOf<TimeoutException>(),
                async () => await Expect().Poll(async () => { calledTimes.Add(DateTime.UtcNow - startTime); await Task.Delay(100); return 0; })
                    .ToBeAsync(1, new() { Intervals = new[] {1000, 200, 2000}}));

            // interval timeout + default delay
            Assert.AreEqual(4, calledTimes.Count);
            Assert.AreEqual(0, calledTimes[0].TotalMilliseconds, 100); // immediate in first poll
            Assert.AreEqual(calledTimes[0].TotalMilliseconds + 1000 + 100, calledTimes[1].TotalMilliseconds, 100);
            Assert.AreEqual(calledTimes[1].TotalMilliseconds + 200 + 100, calledTimes[2].TotalMilliseconds, 100);
            Assert.AreEqual(calledTimes[2].TotalMilliseconds + 2000 + 100, calledTimes[3].TotalMilliseconds, 100);
        }

        [PlaywrightTest("playwright-test/expect-poll.spec.ts", "should respect timeout")]
        public void ShouldTimeOutUsingCustomTimeoutWhenResultNeverMatches()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Assert.ThrowsAsync(Is.TypeOf<TimeoutException>()
                .And.Message.EqualTo("Timeout of 500ms exceeded"),
                async () => await Expect().Poll(async () => { await Task.Delay(100); return 0; }).ToBeAsync(1, new() { Timeout = 500 }));

            stopwatch.Stop();
            Assert.AreEqual(500, stopwatch.ElapsedMilliseconds, 150);
        }

        [PlaywrightTest("playwright-test/expect-poll.spec.ts", "should time out when running infinite predicate")]
        public void ShouldTimeOutWhenRunningInfinitePredicate()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Assert.ThrowsAsync(Is.TypeOf<TimeoutException>()
                .And.Message.EqualTo("Timeout of 100ms exceeded"),
                async () => await Expect().Poll(async () => { await Task.Delay(1000000); return 0; }).ToBeAsync(1, new() { Timeout = 100 }));

            stopwatch.Stop();
            Assert.AreEqual(100, stopwatch.ElapsedMilliseconds, 75);
        }

        [PlaywrightTest()]
        public async Task ShouldPassWhenExpectedIsNull()
        {
            var returnValues = new[] { new TestDto(), null };
            var counter = 0;
            await Expect().Poll(async () => { await Task.Delay(100); return returnValues[counter++]; }).ToBeAsync(null);
        }

        [PlaywrightTest("playwright-test/expect-poll.spec.ts", "should show error that is thrown from predicate")]
        public void ShouldShowErrorThatIsThrownFromPredicate()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var flagToAvoidCompilerWarning = true;
            // For async polling predicate
            Assert.ThrowsAsync(Is.TypeOf<NotImplementedException>().And.Message.EqualTo("Nope"),
                async () => await Expect().Poll(async () =>
                {
                    await Task.Delay(100);
                    if (flagToAvoidCompilerWarning)
                    {
                        throw new NotImplementedException("Nope");
                    }
                    return 1; ;
                }).ToBeAsync(1));

            stopwatch.Stop();
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 200);

            // For sync polling predicate
            Assert.ThrowsAsync(Is.TypeOf<NotImplementedException>().And.Message.EqualTo("Nope"),
                async () => await Expect().Poll(() =>
                {
                    if (flagToAvoidCompilerWarning)
                    {
                        throw new NotImplementedException("Nope");
                    }
                    return 1; ;
                }).ToBeAsync(1));
        }
    }

    public class TestDto
    {

    }
}
