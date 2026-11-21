namespace Stocka.MVC.Localization;

/// <summary>
/// One translatable word/phrase: every language side by side.
/// Add a new language by adding a property here and in <see cref="Loc"/>.
/// </summary>
public readonly record struct L(string Ar, string En);
