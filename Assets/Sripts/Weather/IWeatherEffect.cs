public interface IWeatherEffect
{
    void Begin(WeatherBase data, float duration, int difficulty);
    void End();
}