using System;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
public class RefreshUnitOptionsOnChangeAttribute : Attribute
{
}
