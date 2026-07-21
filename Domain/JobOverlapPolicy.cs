namespace WinCron.Domain;

public enum JobOverlapPolicy
{
    Allow,
    Skip,
    QueueOne,
    TerminatePrevious
}
