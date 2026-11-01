using System;
using System.Collections.Generic;
using System.Text;

namespace FamilyManagement.Domain.Common;

// Criei esta interface para definir um contrato comum para as entidades
// que podem ser desativadas sem serem apagadas da base de dados.
// -------------------//
// Optei pela desativação porque preciso de preservar o histórico
// financeiro e as referências entre os registos, mesmo quando
// deixam de estar disponíveis para novas operações.

public interface ISoftDeletable
{
    // Utilizo esta propriedade para identificar se o registo está ativo.
    bool IsActive { get; }

    // Defini este método para que cada implementação disponibilize
    // uma forma de desativar o registo.
    // A gravação dessa alteração é tratada noutra camada.
    void Deactivate();        
}

