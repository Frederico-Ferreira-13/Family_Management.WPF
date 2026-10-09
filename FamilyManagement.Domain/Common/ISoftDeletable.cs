using System;
using System.Collections.Generic;
using System.Text;

namespace FamilyManagement.Domain.Common;

public interface ISoftDeletable
{
    bool IsActive { get; }       
    void Deactivate();        
}

