﻿using Annular.Translate.Primitives;

namespace Annular.Translate.Abstract;

public abstract class TranslateCompiler
{
    public abstract string Compile(string value, string lang);

    public abstract Translations CompileTranslations(Translations translations, string lang);
}
