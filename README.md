# UnificaSUS - Aplicação

## 📁 Estrutura da Pasta

Esta pasta contém apenas os arquivos essenciais para o funcionamento da aplicação UnificaSUS.

### Arquivos Essenciais

- **`unificasus.exe`** - Aplicação principal
- **`gds32.dll`** - Biblioteca do Firebird (32-bit, versão 5.0.3.1683)
- **`DelZip179.dll`** - Biblioteca de compressão
- **`unificasus.ini`** - Arquivo de configuração
- **`UNIFICASUS.GDB`** - Banco de dados Firebird
- **`firebird.log`** - Log do Firebird

### Pasta `old`

Todos os arquivos de documentação, scripts, backups e arquivos temporários foram movidos para a pasta `old` para manter a organização.

**Conteúdo da pasta `old`:**
- Documentação (.md)
- Scripts PowerShell (.ps1)
- Scripts SQL (.sql)
- Arquivos de texto (.txt)
- Código fonte (.pas)
- Backups do gds32.dll
- Arquivos compactados (.zip)
- Pastas temporárias (temp, MIGRAÇÃO, etc.)

## ⚙️ Configuração

O arquivo `unificasus.ini` contém a configuração de conexão com o banco de dados:

```ini
[DB]
local=192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB
```

## 🔧 Requisitos

- **Firebird 5.0** instalado no sistema
- **Visual C++ Redistributable 2015-2022 (x86)** instalado
- **gds32.dll** na pasta da aplicação (32-bit)
- **gds32.dll** em `C:\Windows\SysWOW64` (32-bit)

## 📝 Notas

- A aplicação é **32-bit (x86)**
- O `gds32.dll` deve ser a versão **32-bit** do Firebird 5.0
- A pasta foi organizada em: **14/11/2024**

## 🆘 Problemas?

Se encontrar problemas, consulte a documentação na pasta `old`:
- `old/SOLUCAO_FINAL_32BIT.md` - Solução para erro de DLL
- `old/SOLUCAO_DEFINITIVA_GDS32.md` - Soluções adicionais

---

**Última organização**: 14/11/2024

