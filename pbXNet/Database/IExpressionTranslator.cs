﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace pbXNet.Database
{
	public interface IExpressionTranslator
	{
		IExpressionTranslator New(Type typeForWhichMemberNamesWillBeEmitted = null);

		List<(string name, object value)> Parameters { get; }

		string Translate(Expression expr);
	}
}
