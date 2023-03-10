using Timer = System.Timers.Timer;

namespace DiscordBot.Services;

/// <summary>
///     A global timer with timer ids and cross command use
/// </summary>
public class GuildTimer
{
    private readonly List<Timed> _timedList = new();

    private string GenerateUniqueId()
    {
        string id;
        do
        {
            id = Util.RandomString(10);
        } while (_timedList.ConvertAll(t => t.Id).Contains(id));

        return id;
    }

    /// <summary>
    ///     Sets a new callback of a timer instance. Uses an existing instance when the ids matches (stops old timer)
    /// </summary>
    /// <param name="cb">The callback when the timer resolves</param>
    /// <param name="id">The timers id. If null a random, unique id will be generated</param>
    /// <returns>A timed instance</returns>
    public Timed Run(Action cb, string? id = null)
    {
        id ??= GenerateUniqueId();

        var existing = Get(id);
        if (existing == null)
        {
            var timer = new Timed(cb, id);
            _timedList.Add(timer);
            return timer;
        }

        existing.Stop();
        existing.Callback = cb;

        return existing;
    }


    /// <summary>
    ///     Stops all timers with the given id
    /// </summary>
    /// <param name="id">The timers id</param>
    public void Stop(string id)
    {
        _timedList.FindAll(f => f.Id == id).ForEach(t => t.Stop());
    }

    /// <summary>
    ///     Gets a timer with a given id.
    /// </summary>
    /// <param name="id">The timers id</param>
    /// <returns>The instance of the timer or null if none if a matching id exists</returns>
    private Timed? Get(string id)
    {
        return _timedList.Find(t => t.Id == id);
    }

    public class Timed
    {
        private bool _repeat;
        private long _time = 100;
        private Timer? _timer;
        public Action Callback;

        public Timed(Action cb, string id)
        {
            Id = id;
            Callback = cb;
        }

        public bool Running => _timer?.Enabled ?? false;

        public string Id { get; }

        /// <summary>
        ///     Defines the time of the timer. Milliseconds, seconds and minutes will add up.
        /// </summary>
        /// <param name="milliseconds">The number of milliseconds</param>
        /// <param name="seconds">The number of seconds</param>
        /// <param name="minutes">The number of minutes</param>
        /// <returns>The Timed instance</returns>
        public Timed In(long milliseconds = 0, int seconds = 0, int minutes = 0)
        {
            _time = milliseconds + seconds * 1000 + minutes * 60 * 1000;
            _repeat = false;
            return this;
        }

        /// <summary>
        ///     See <see cref="Timed.In">Timed.In</see>, but the timer repeats
        /// </summary>
        /// <param name="milliseconds">The number of milliseconds</param>
        /// <param name="seconds">The number of seconds</param>
        /// <param name="minutes">The number of minutes</param>
        /// <returns>The Timed instance</returns>
        public Timed Every(long milliseconds = 0, int seconds = 0, int minutes = 0)
        {
            _time = milliseconds + seconds * 1000 + minutes * 60 * 1000;
            _repeat = true;
            return this;
        }

        /// <summary>
        ///     Stops the running timer
        /// </summary>
        public void Stop()
        {
            _timer?.Stop();
        }

        /// <summary>
        ///     Starts the timer
        /// </summary>
        /// <returns></returns>
        public Timed Start()
        {
            _timer = new Timer(_time);
            _timer.AutoReset = false;
            _timer.Elapsed += (_, _) => Callback();
            _timer.Enabled = true;
            _timer.AutoReset = _repeat;
            return this;
        }
    }
}