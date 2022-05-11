using System.Drawing;
using System.Threading.Channels;

namespace queue.service.Services;

public class WriterService : BackgroundService
{
    private readonly ChannelWriter<Car> _channelWriter;

    public WriterService(ChannelWriter<Car> channelWriter)
    {
        _channelWriter = channelWriter;
    }

    private string GetRandomColor()
    {
        var colors = new[] { "Red", "Cyan", "Yellow", "Green", "Blue", "Magenta", "Gray", "White" };
        Random random = new Random();
        return colors[random.Next(0, colors.Length)];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int counter = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            if (counter % 1000 == 0)
            {
                Console.WriteLine($"Batch {(counter / 1000)} loaded...");

                if (counter == 1000) { break; }
            }
            await Task.Delay(1, stoppingToken);

            var car = new Car
            {
                Color = GetRandomColor()
            };

            await _channelWriter.WriteAsync(car, stoppingToken).ConfigureAwait(false);
            counter++;
        }
    }
}

public class MultipleReaderService : BackgroundService
{
    private readonly ChannelReader<Car> _channelReader;
    private Dictionary<string, int> _carCounts = new();

    public MultipleReaderService(ChannelReader<Car> channelReader)
    {
        _channelReader = channelReader;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int counter = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
            try
            {
                await foreach (var car in _channelReader.ReadAllAsync(stoppingToken))
                {
                    counter++;

                    if (_carCounts.ContainsKey(car.Color))
                    {
                        _carCounts[car.Color]++;
                    }
                    else
                    {
                        _carCounts.Add(car.Color, 1);
                    }

                    if (counter % 10 == 0)
                    {
                        Console.Clear();

                        var temp = Console.ForegroundColor;
                        Console.WriteLine($"Received {counter} cars");

                        foreach (var count in _carCounts)
                        {
                            var color = Enum.Parse<ConsoleColor>(count.Key);
                            Console.ForegroundColor = color;

                            Console.WriteLine($"{"".PadLeft(count.Value, '█')}|{count.Key}\t{count.Value}");                            
                        }

                        Console.ForegroundColor = temp;
                    }

                    

                    await Task.Delay(5);
                }
            }
            catch (ChannelClosedException)
            {
            }                    
        }
    }
}

public class SingleReaderService : BackgroundService
{
    private readonly ChannelReader<Car> _channelReader;
    private Dictionary<string, int> _carCounts = new();

    public SingleReaderService(ChannelReader<Car> channelReader)
    {
        _channelReader = channelReader;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
            try
            {
                var result = await _channelReader.ReadAsync(stoppingToken).ConfigureAwait(false);
                if (_carCounts.ContainsKey(result.Color))
                {
                    _carCounts[result.Color]++;
                }
                else
                {
                    _carCounts.Add(result.Color, 1);
                }
            }
            catch (ChannelClosedException)
            {
            }

            foreach (var count in _carCounts)
            {
                Console.WriteLine($"{count.Key}\t{count.Value}");
            }

            await Task.Delay(2000);
            Console.Clear();
        }
    }
}

public record Car
{
    public string Color { get; init; } = default;
}