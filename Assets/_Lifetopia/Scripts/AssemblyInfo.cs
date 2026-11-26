using System.Runtime.CompilerServices;

// Lets EditMode tests build in-memory item definitions without exposing setters to gameplay code.
[assembly: InternalsVisibleTo("Lifetopia.Tests.EditMode")]
