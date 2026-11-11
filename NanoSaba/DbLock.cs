namespace NanoSaba;

// Blazor components on the same page share ONE database connection (DbContext).
// A DbContext can do only one thing at a time: if two components ask the database
// at the same moment, one of them gets an error.
// So each component waits for this lock before it asks the database.
// AddScoped = one lock per visitor (per page connection), not one for the whole site.
public class DbLock
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
}
