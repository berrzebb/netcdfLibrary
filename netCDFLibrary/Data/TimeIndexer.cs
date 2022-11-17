namespace netCDFLibrary.Data
{
    public record TimeIndex(DateTime date, long Ticks, int index);
    public class TimeIndexer
    {
        public static readonly TimeIndexer Empty = new(Array.Empty<DateTime>());
        private readonly DateTime[] timeIndex;
        private IEnumerable<TimeIndex> Transform(DateTime dateTime) => this.timeIndex.Select((date, index) => new TimeIndex(date, (date - dateTime).Ticks, index));
        private TimeIndexer(params DateTime[] timeIndex)
        {
            this.timeIndex = timeIndex;
        }
        public int Count => this.timeIndex.Length;
        public DateTime[] DateTimes => this.timeIndex;
        public DateTime[] GetDates()
        {
            return this.timeIndex.Select(v => v.Date).Distinct().ToArray();
        }
        public (TimeIndexer indexer,TimeIndex? index) this[DateTime dateTime]
        {
            get {
                var index = this.Transform(dateTime)
                    .Where(v => v.Ticks >= 0)
                    .MinBy(t => Math.Abs((t.date - dateTime).Ticks));
                return (this,index);
            }
        }
        public DateTime this[int index]
        {
            get
            {
                if (index > this.timeIndex.Length || index < 0)
                {
                    throw new IndexOutOfRangeException($"TimeIndexer Index Out Of Range Current: {index} Actual: {this.timeIndex.Length}");
                }

                return this.timeIndex[index];
            }
        }
        public bool Contains(DateTime dateTime)
        {
            return this.Transform(dateTime).Any(v => v.Ticks >= 0);
        }
        public static TimeIndexer Create(params DateTime[] timeIndex)
        {
            return new TimeIndexer(timeIndex);
        }
    }
}
