// The clock seam (ValidationUtils.Clock) is process-global static state, so tests that
// swap it must not run in parallel with tests that read the current date. The suite is
// tiny, so serial execution is the simplest safe choice.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
