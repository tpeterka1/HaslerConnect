namespace HaslerConnect;

public abstract class GameModule
{
    public abstract void Initialize(); // Initializes needed methods for the specific game (RailDriver for RW, OCR for TD2 etc.)
    public abstract bool ReadyForRead(); // Returns if sim is ready for speed reading
    public abstract int GetSpeed(); // Method which returns the speed in kph
    public abstract void Tick(); // Runs every loop cycle (every 100 ms), used for additional tasks (loco change checks etc.)
}