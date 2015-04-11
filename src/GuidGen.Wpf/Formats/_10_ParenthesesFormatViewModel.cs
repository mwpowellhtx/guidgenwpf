﻿using System;

namespace GuidGen.Formats
{
    [DisplayOrder(10)]
    public class ParenthesesFormatViewModel : FormatViewModel
    {
        private static string Formatter(Guid value, TextCase textCase)
        {
            return value.ToString("P").ToTextCase(textCase);
        }

        public ParenthesesFormatViewModel(IGeneratorOptions options)
            : base(options, @"Parentheses", Formatter)
        {
        }
    }
}
