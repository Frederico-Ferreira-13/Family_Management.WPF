using System;
using System.Collections.Generic;
using System.Text;

namespace FamilyManagement.Domain.Errors;

public enum ErrorType
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    BusinessRuleViolation = 4,
    Unauthorized = 5,
    Internal = 6
}