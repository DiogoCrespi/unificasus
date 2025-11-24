# Verificação dos Campos VL_TA e VL_TH

## 📋 Situação Atual

### 1. **Código C# - Entidade Procedimento**
✅ Os campos **existem** na entidade:
- `VlTa` (decimal?)
- `VlTh` (decimal?)

**Localização:** `UnificaSUS.Core/Entities/Procedimento.cs` (linhas 20-21)

### 2. **Código C# - Repository**
⚠️ Os campos **NÃO estão sendo selecionados** nas queries SQL:
- Todas as queries SELECT em `ProcedimentoRepository.cs` selecionam apenas:
  - `VL_SH`
  - `VL_SA`
  - `VL_SP`
- **VL_TA e VL_TH não estão no SELECT**

**Localização:** `ProcedimentoRepository.cs` (linhas 30-48, 97-115, etc.)

### 3. **Código C# - Mapeamento**
✅ O código tenta ler os campos usando `TryGetDecimal`:
```csharp
VlTa = TryGetDecimal(reader, "VL_TA"),
VlTh = TryGetDecimal(reader, "VL_TH"),
```

**Problema:** Como os campos não estão no SELECT, o `TryGetDecimal` vai lançar exceção ao tentar obter o ordinal.

**Localização:** `ProcedimentoRepository.cs` (linhas 527-528)

### 4. **Código C# - Interface (MainWindow)**
✅ O código já tem lógica de fallback:
```csharp
// Total Ambulatorial (T.A.) = VL_SA + VL_SP (se VL_TA não existir no banco)
// Total Hospitalar (T.H.) = VL_SH (se VL_TH não existir no banco)
var totalAmbulatorial = procedimento.VlTa ?? (procedimento.VlSa ?? 0) + (procedimento.VlSp ?? 0);
var totalHospitalar = procedimento.VlTh ?? procedimento.VlSh ?? 0;
```

**Localização:** `MainWindow.xaml.cs` (linhas 440-443)

## 🔍 Verificação Necessária

### Script SQL Criado
Criei o arquivo `verificar_campos_vl_ta_th.sql` para verificar se os campos existem no banco de dados.

**Execute o script no banco Firebird para verificar:**
1. Se os campos VL_TA e VL_TH existem na tabela TB_PROCEDIMENTO
2. Se existem, qual o tipo de dados
3. Se há dados nesses campos

## ✅ Ações Necessárias

### Se os campos **EXISTIREM** no banco:
1. Adicionar `VL_TA` e `VL_TH` ao SELECT em todas as queries do `ProcedimentoRepository.cs`
2. Manter a lógica de fallback no `MainWindow.xaml.cs` (caso os campos estejam NULL)

### Se os campos **NÃO EXISTIREM** no banco:
1. Remover a tentativa de leitura dos campos no `ProcedimentoRepository.cs`
2. Manter apenas a lógica de cálculo no `MainWindow.xaml.cs`
3. Opcionalmente, adicionar os campos ao banco se necessário

## 📝 Queries que Precisam ser Atualizadas (se os campos existirem)

Todas as queries SELECT em `ProcedimentoRepository.cs`:
- `BuscarTodosAsync` (linha ~30)
- `BuscarPorCodigoAsync` (linha ~97)
- `BuscarPorFormaOrganizacaoAsync` (linha ~155)
- `BuscarPorSubGrupoAsync` (linha ~210)
- `BuscarPorGrupoAsync` (linha ~263)
- `BuscarPorCompetenciaAsync` (linha ~343)

**Adicionar após `pr.VL_SP`:**
```sql
pr.VL_TA,
pr.VL_TH,
```

## 🧪 Como Testar

1. Execute o script `verificar_campos_vl_ta_th.sql` no banco
2. Verifique o resultado:
   - Se retornar linhas com VL_TA e VL_TH → campos existem
   - Se não retornar nada → campos não existem
3. Com base no resultado, aplique as correções necessárias



