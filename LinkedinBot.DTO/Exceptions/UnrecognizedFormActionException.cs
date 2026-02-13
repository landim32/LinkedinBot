namespace LinkedinBot.DTO.Exceptions;

public class UnrecognizedFormActionException : Exception
{
    public int Step { get; }

    public UnrecognizedFormActionException(int step)
        : base($"No recognized action button found at step {step}.")
    {
        Step = step;
    }
}
