using Xunit;

namespace VPet_Simulator.Core.Test.Handle;

public class CalModeTests
{
    [Theory]
    // High health cases
    [InlineData(90, 90, 60, GameSave.ModeType.Happy)] // High health, high feeling, high likability
    [InlineData(50, 90, 60, GameSave.ModeType.Happy)] // Moderate health, high feeling, high likability
    [InlineData(90, 50, 60, GameSave.ModeType.Nomal)] // High health, moderate feeling, high likability
    [InlineData(50, 50, 60, GameSave.ModeType.PoorCondition)] // Moderate health, moderate feeling, high likability
    // Low health cases
    [InlineData(30, 90, 60, GameSave.ModeType.PoorCondition)] // Low health, high feeling, high likability
    [InlineData(30, 50, 60, GameSave.ModeType.PoorCondition)] // Low health, moderate feeling, high likability
    [InlineData(15, 90, 60, GameSave.ModeType.Ill)] // Very low health, high feeling, high likability
    [InlineData(15, 50, 60, GameSave.ModeType.Ill)] // Very low health, moderate feeling, high likability
    [InlineData(15, 10, 60, GameSave.ModeType.Ill)] // Very low health, very low feeling, high likability
    // Low likability cases
    [InlineData(90, 90, 10, GameSave.ModeType.Happy)] // High health, high feeling, very low likability
    [InlineData(50, 90, 10, GameSave.ModeType.Happy)] // Moderate health, high feeling, very low likability
    [InlineData(90, 50, 10, GameSave.ModeType.Nomal)] // High health, moderate feeling, very low likability
    [InlineData(50, 50, 10, GameSave.ModeType.PoorCondition)] // Moderate health, moderate feeling, very low likability
    [InlineData(30, 90, 10, GameSave.ModeType.PoorCondition)] // Low health, high feeling, very low likability
    [InlineData(30, 50, 10, GameSave.ModeType.Ill)] // Low health, moderate feeling, very low likability
    [InlineData(15, 90, 10, GameSave.ModeType.Ill)] // Very low health, high feeling, very low likability
    [InlineData(15, 50, 10, GameSave.ModeType.Ill)] // Very low health, moderate feeling, very low likability
    [InlineData(15, 10, 10, GameSave.ModeType.Ill)] // Very low health, very low feeling, very low likability
    // Middle cases
    [InlineData(60, 60, 30, GameSave.ModeType.PoorCondition)] // Moderate health, moderate feeling, moderate likability
    [InlineData(75, 75, 75,
        GameSave.ModeType.Nomal)] // Moderate-high health, moderate-high feeling, moderate-high likability
    // Add more test cases as needed
    public void CalMode_ReturnsExpectedMode(int health, int feeling, int likability, GameSave.ModeType expectedMode)
    {
        // Arrange
        var testInstance = new GameSave
        {
            Health = health,
            Feeling = feeling,
            Likability = likability
        }; // Replace with the actual class containing CalMode method

        // Act
        var result = testInstance.CalMode();

        // Assert
        Assert.Equal(expectedMode, result);
    }
}