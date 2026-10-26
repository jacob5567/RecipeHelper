using System.Runtime.CompilerServices;

// The dev-only RecipeHelper.Tests plugin reads the planner's output. It is never shipped.
[assembly: InternalsVisibleTo("RecipeHelper.Tests")]
