IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260321185618_InitialCreate'
)
BEGIN
    CREATE TABLE [Clientes] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Senha] nvarchar(max) NOT NULL,
        [CPF] nvarchar(max) NULL,
        [Telefone] nvarchar(max) NULL,
        [Genero] nvarchar(max) NULL,
        [DataNascimento] datetime2 NULL,
        [IsAdmin] bit NOT NULL,
        CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260321185618_InitialCreate'
)
BEGIN
    CREATE TABLE [Estados] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(max) NOT NULL,
        [Sigla] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Estados] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260321185618_InitialCreate'
)
BEGIN
    CREATE TABLE [Cidades] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(max) NOT NULL,
        [EstadoId] int NOT NULL,
        CONSTRAINT [PK_Cidades] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Cidades_Estados_EstadoId] FOREIGN KEY ([EstadoId]) REFERENCES [Estados] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260321185618_InitialCreate'
)
BEGIN
    CREATE TABLE [Bairros] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(max) NOT NULL,
        [CidadeId] int NOT NULL,
        CONSTRAINT [PK_Bairros] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Bairros_Cidades_CidadeId] FOREIGN KEY ([CidadeId]) REFERENCES [Cidades] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260321185618_InitialCreate'
)
BEGIN
    CREATE TABLE [Enderecos] (
        [Id] int NOT NULL IDENTITY,
        [NomeEndereco] nvarchar(max) NOT NULL,
        [CEP] nvarchar(max) NOT NULL,
        [Logradouro] nvarchar(max) NOT NULL,
        [Numero] nvarchar(max) NOT NULL,
        [Complemento] nvarchar(max) NULL,
        [CidadeId] int NOT NULL,
        [BairroId] int NOT NULL,
        [ClienteId] int NOT NULL,
        CONSTRAINT [PK_Enderecos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Enderecos_Bairros_BairroId] FOREIGN KEY ([BairroId]) REFERENCES [Bairros] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Enderecos_Cidades_CidadeId] FOREIGN KEY ([CidadeId]) REFERENCES [Cidades] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Enderecos_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260321185618_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bairros_CidadeId] ON [Bairros] ([CidadeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260321185618_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Cidades_EstadoId] ON [Cidades] ([EstadoId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260321185618_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Enderecos_BairroId] ON [Enderecos] ([BairroId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260321185618_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Enderecos_CidadeId] ON [Enderecos] ([CidadeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260321185618_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Enderecos_ClienteId] ON [Enderecos] ([ClienteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260321185618_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260321185618_InitialCreate', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260321225032_AddEnderecoPadrao'
)
BEGIN
    ALTER TABLE [Enderecos] ADD [IsPadrao] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260321225032_AddEnderecoPadrao'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260321225032_AddEnderecoPadrao', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260321231417_AddCartoes'
)
BEGIN
    CREATE TABLE [Cartoes] (
        [Id] int NOT NULL IDENTITY,
        [NomeImpresso] nvarchar(max) NOT NULL,
        [Numero] nvarchar(max) NOT NULL,
        [Validade] nvarchar(max) NOT NULL,
        [CVV] nvarchar(max) NOT NULL,
        [IsPadrao] bit NOT NULL,
        [ClienteId] int NOT NULL,
        CONSTRAINT [PK_Cartoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Cartoes_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260321231417_AddCartoes'
)
BEGIN
    CREATE INDEX [IX_Cartoes_ClienteId] ON [Cartoes] ([ClienteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260321231417_AddCartoes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260321231417_AddCartoes', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260322172918_AddIsAtivoCliente'
)
BEGIN
    ALTER TABLE [Clientes] ADD [IsAtivo] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260322172918_AddIsAtivoCliente'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260322172918_AddIsAtivoCliente', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260404192843_CriarLivrosECategorias'
)
BEGIN
    CREATE TABLE [Categorias] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Categorias] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260404192843_CriarLivrosECategorias'
)
BEGIN
    CREATE TABLE [Livros] (
        [Id] int NOT NULL IDENTITY,
        [Titulo] nvarchar(max) NOT NULL,
        [Ano] int NOT NULL,
        [Autor] nvarchar(max) NOT NULL,
        [Editora] nvarchar(max) NOT NULL,
        [Edicao] nvarchar(max) NOT NULL,
        [ISBN] nvarchar(max) NOT NULL,
        [CodigoBarras] nvarchar(max) NOT NULL,
        [NumeroPaginas] int NOT NULL,
        [Sinopse] nvarchar(max) NOT NULL,
        [Altura] decimal(18,2) NOT NULL,
        [Largura] decimal(18,2) NOT NULL,
        [Peso] decimal(18,2) NOT NULL,
        [Profundidade] decimal(18,2) NOT NULL,
        [Preco] decimal(18,2) NOT NULL,
        [ImagemUrl] nvarchar(max) NOT NULL,
        [IsAtivo] bit NOT NULL,
        CONSTRAINT [PK_Livros] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260404192843_CriarLivrosECategorias'
)
BEGIN
    CREATE TABLE [CategoriaLivro] (
        [CategoriasId] int NOT NULL,
        [LivrosId] int NOT NULL,
        CONSTRAINT [PK_CategoriaLivro] PRIMARY KEY ([CategoriasId], [LivrosId]),
        CONSTRAINT [FK_CategoriaLivro_Categorias_CategoriasId] FOREIGN KEY ([CategoriasId]) REFERENCES [Categorias] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CategoriaLivro_Livros_LivrosId] FOREIGN KEY ([LivrosId]) REFERENCES [Livros] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260404192843_CriarLivrosECategorias'
)
BEGIN
    CREATE INDEX [IX_CategoriaLivro_LivrosId] ON [CategoriaLivro] ([LivrosId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260404192843_CriarLivrosECategorias'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260404192843_CriarLivrosECategorias', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260404195451_ValidacoesLivro'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Livros]') AND [c].[name] = N'Titulo');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Livros] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Livros] ALTER COLUMN [Titulo] nvarchar(200) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260404195451_ValidacoesLivro'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Livros]') AND [c].[name] = N'Autor');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Livros] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Livros] ALTER COLUMN [Autor] nvarchar(100) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260404195451_ValidacoesLivro'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260404195451_ValidacoesLivro', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405181024_CriarPedido'
)
BEGIN
    CREATE TABLE [Pedidos] (
        [Id] int NOT NULL IDENTITY,
        [ClienteId] int NOT NULL,
        [EnderecoId] int NOT NULL,
        [Data] datetime2 NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Pedidos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Pedidos_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Pedidos_Enderecos_EnderecoId] FOREIGN KEY ([EnderecoId]) REFERENCES [Enderecos] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405181024_CriarPedido'
)
BEGIN
    CREATE TABLE [Pagamentos] (
        [Id] int NOT NULL IDENTITY,
        [PedidoId] int NOT NULL,
        [Metodo] nvarchar(max) NOT NULL,
        [Valor] decimal(18,2) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Pagamentos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Pagamentos_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405181024_CriarPedido'
)
BEGIN
    CREATE TABLE [PedidoItens] (
        [Id] int NOT NULL IDENTITY,
        [PedidoId] int NOT NULL,
        [LivroId] int NOT NULL,
        [Quantidade] int NOT NULL,
        [PrecoUnitario] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_PedidoItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PedidoItens_Livros_LivroId] FOREIGN KEY ([LivroId]) REFERENCES [Livros] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PedidoItens_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405181024_CriarPedido'
)
BEGIN
    CREATE INDEX [IX_Pagamentos_PedidoId] ON [Pagamentos] ([PedidoId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405181024_CriarPedido'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_LivroId] ON [PedidoItens] ([LivroId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405181024_CriarPedido'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_PedidoId] ON [PedidoItens] ([PedidoId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405181024_CriarPedido'
)
BEGIN
    CREATE INDEX [IX_Pedidos_ClienteId] ON [Pedidos] ([ClienteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405181024_CriarPedido'
)
BEGIN
    CREATE INDEX [IX_Pedidos_EnderecoId] ON [Pedidos] ([EnderecoId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405181024_CriarPedido'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260405181024_CriarPedido', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405193934_CreateEstoque'
)
BEGIN
    CREATE TABLE [Estoques] (
        [Id] int NOT NULL IDENTITY,
        [LivroId] int NOT NULL,
        [Quantidade] int NOT NULL,
        [QuantidadeMinima] int NOT NULL,
        CONSTRAINT [PK_Estoques] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Estoques_Livros_LivroId] FOREIGN KEY ([LivroId]) REFERENCES [Livros] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405193934_CreateEstoque'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Estoques_LivroId] ON [Estoques] ([LivroId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405193934_CreateEstoque'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260405193934_CreateEstoque', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406145611_AddTrocasECupons'
)
BEGIN
    CREATE TABLE [CuponsDesconto] (
        [Id] int NOT NULL IDENTITY,
        [Codigo] nvarchar(max) NOT NULL,
        [Valor] decimal(18,2) NOT NULL,
        [Tipo] nvarchar(max) NOT NULL,
        [IsAtivo] bit NOT NULL,
        [DataCriacao] datetime2 NOT NULL,
        [DataUtilizacao] datetime2 NULL,
        [ClienteId] int NULL,
        [PedidoId] int NULL,
        [TrocaId] int NULL,
        CONSTRAINT [PK_CuponsDesconto] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CuponsDesconto_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CuponsDesconto_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406145611_AddTrocasECupons'
)
BEGIN
    CREATE TABLE [Trocas] (
        [Id] int NOT NULL IDENTITY,
        [Codigo] nvarchar(max) NOT NULL,
        [PedidoId] int NOT NULL,
        [PedidoItemId] int NOT NULL,
        [ClienteId] int NOT NULL,
        [Motivo] nvarchar(max) NOT NULL,
        [ObservacaoCliente] nvarchar(max) NULL,
        [ObservacaoAdmin] nvarchar(max) NULL,
        [Status] nvarchar(max) NOT NULL,
        [DataSolicitacao] datetime2 NOT NULL,
        [DataAnalise] datetime2 NULL,
        [CupomDescontoId] int NULL,
        CONSTRAINT [PK_Trocas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Trocas_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Trocas_CuponsDesconto_CupomDescontoId] FOREIGN KEY ([CupomDescontoId]) REFERENCES [CuponsDesconto] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Trocas_PedidoItens_PedidoItemId] FOREIGN KEY ([PedidoItemId]) REFERENCES [PedidoItens] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Trocas_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406145611_AddTrocasECupons'
)
BEGIN
    CREATE INDEX [IX_CuponsDesconto_ClienteId] ON [CuponsDesconto] ([ClienteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406145611_AddTrocasECupons'
)
BEGIN
    CREATE INDEX [IX_CuponsDesconto_PedidoId] ON [CuponsDesconto] ([PedidoId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406145611_AddTrocasECupons'
)
BEGIN
    CREATE INDEX [IX_Trocas_ClienteId] ON [Trocas] ([ClienteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406145611_AddTrocasECupons'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Trocas_CupomDescontoId] ON [Trocas] ([CupomDescontoId]) WHERE [CupomDescontoId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406145611_AddTrocasECupons'
)
BEGIN
    CREATE INDEX [IX_Trocas_PedidoId] ON [Trocas] ([PedidoId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406145611_AddTrocasECupons'
)
BEGIN
    CREATE INDEX [IX_Trocas_PedidoItemId] ON [Trocas] ([PedidoItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406145611_AddTrocasECupons'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260406145611_AddTrocasECupons', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407113000_AddCarrinhoPersistidoCliente'
)
BEGIN
    ALTER TABLE [Clientes] ADD [CarrinhoPersistidoJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407113000_AddCarrinhoPersistidoCliente'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260407113000_AddCarrinhoPersistidoCliente', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408204449_AddTrocaRecebimentoFluxo'
)
BEGIN
    ALTER TABLE [Trocas] ADD [DataRecebimento] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408204449_AddTrocaRecebimentoFluxo'
)
BEGIN
    ALTER TABLE [Trocas] ADD [RetornarAoEstoque] bit NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408204449_AddTrocaRecebimentoFluxo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260408204449_AddTrocaRecebimentoFluxo', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411133014_AddEnderecoTiposEFinalidades'
)
BEGIN
    ALTER TABLE [Enderecos] ADD [IsCobranca] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411133014_AddEnderecoTiposEFinalidades'
)
BEGIN
    ALTER TABLE [Enderecos] ADD [IsEntrega] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411133014_AddEnderecoTiposEFinalidades'
)
BEGIN
    ALTER TABLE [Enderecos] ADD [Pais] nvarchar(max) NOT NULL DEFAULT N'Brasil';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411133014_AddEnderecoTiposEFinalidades'
)
BEGIN
    ALTER TABLE [Enderecos] ADD [TipoLogradouro] nvarchar(max) NOT NULL DEFAULT N'Rua';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411133014_AddEnderecoTiposEFinalidades'
)
BEGIN
    ALTER TABLE [Enderecos] ADD [TipoResidencia] nvarchar(max) NOT NULL DEFAULT N'Casa';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411133014_AddEnderecoTiposEFinalidades'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260411133014_AddEnderecoTiposEFinalidades', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411142527_AddBandeirasCartao'
)
BEGIN
    ALTER TABLE [Cartoes] ADD [BandeiraCartaoId] int NOT NULL DEFAULT 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411142527_AddBandeirasCartao'
)
BEGIN
    CREATE TABLE [BandeirasCartao] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(max) NOT NULL,
        [Codigo] nvarchar(450) NOT NULL,
        [IsAtiva] bit NOT NULL,
        CONSTRAINT [PK_BandeirasCartao] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411142527_AddBandeirasCartao'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Codigo', N'IsAtiva', N'Nome') AND [object_id] = OBJECT_ID(N'[BandeirasCartao]'))
        SET IDENTITY_INSERT [BandeirasCartao] ON;
    EXEC(N'INSERT INTO [BandeirasCartao] ([Id], [Codigo], [IsAtiva], [Nome])
    VALUES (1, N''VISA'', CAST(1 AS bit), N''Visa''),
    (2, N''MASTERCARD'', CAST(1 AS bit), N''Mastercard''),
    (3, N''ELO'', CAST(1 AS bit), N''Elo''),
    (4, N''HIPERCARD'', CAST(1 AS bit), N''Hipercard''),
    (5, N''AMEX'', CAST(1 AS bit), N''American Express'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Codigo', N'IsAtiva', N'Nome') AND [object_id] = OBJECT_ID(N'[BandeirasCartao]'))
        SET IDENTITY_INSERT [BandeirasCartao] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411142527_AddBandeirasCartao'
)
BEGIN
    UPDATE Cartoes SET BandeiraCartaoId = 1 WHERE ISNULL(BandeiraCartaoId, 0) = 0
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411142527_AddBandeirasCartao'
)
BEGIN
    CREATE INDEX [IX_Cartoes_BandeiraCartaoId] ON [Cartoes] ([BandeiraCartaoId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411142527_AddBandeirasCartao'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BandeirasCartao_Codigo] ON [BandeirasCartao] ([Codigo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411142527_AddBandeirasCartao'
)
BEGIN
    ALTER TABLE [Cartoes] ADD CONSTRAINT [FK_Cartoes_BandeirasCartao_BandeiraCartaoId] FOREIGN KEY ([BandeiraCartaoId]) REFERENCES [BandeirasCartao] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411142527_AddBandeirasCartao'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260411142527_AddBandeirasCartao', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415223814_AddReservaCarrinho'
)
BEGIN
    CREATE TABLE [ReservasCarrinho] (
        [Id] int NOT NULL IDENTITY,
        [LivroId] int NOT NULL,
        [ClienteId] int NULL,
        [SessionKey] nvarchar(450) NULL,
        [Quantidade] int NOT NULL,
        [ReservadoEm] datetime2 NOT NULL,
        [ExpiraEm] datetime2 NOT NULL,
        CONSTRAINT [PK_ReservasCarrinho] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReservasCarrinho_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ReservasCarrinho_Livros_LivroId] FOREIGN KEY ([LivroId]) REFERENCES [Livros] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415223814_AddReservaCarrinho'
)
BEGIN
    CREATE INDEX [IX_ReservasCarrinho_ClienteId_SessionKey] ON [ReservasCarrinho] ([ClienteId], [SessionKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415223814_AddReservaCarrinho'
)
BEGIN
    CREATE INDEX [IX_ReservasCarrinho_LivroId_ExpiraEm] ON [ReservasCarrinho] ([LivroId], [ExpiraEm]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415223814_AddReservaCarrinho'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260415223814_AddReservaCarrinho', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260426135124_AddTipoEntregaPedido'
)
BEGIN
    ALTER TABLE [Pedidos] ADD [TipoEntrega] nvarchar(20) NOT NULL DEFAULT N'PADRAO';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260426135124_AddTipoEntregaPedido'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260426135124_AddTipoEntregaPedido', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427142351_AddDataEntregaPrevistaPedido'
)
BEGIN
    ALTER TABLE [Pedidos] ADD [DataEntregaPrevista] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427142351_AddDataEntregaPrevistaPedido'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260427142351_AddDataEntregaPrevistaPedido', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615223129_AddWishlist'
)
BEGIN
    CREATE TABLE [Wishlists] (
        [Id] int NOT NULL IDENTITY,
        [ClienteId] int NOT NULL,
        [IsAtiva] bit NOT NULL,
        [DataCriacao] datetime2 NOT NULL,
        CONSTRAINT [PK_Wishlists] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Wishlists_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615223129_AddWishlist'
)
BEGIN
    CREATE TABLE [WishlistItems] (
        [Id] int NOT NULL IDENTITY,
        [WishlistId] int NOT NULL,
        [LivroId] int NOT NULL,
        [DataAdicao] datetime2 NOT NULL,
        CONSTRAINT [PK_WishlistItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WishlistItems_Livros_LivroId] FOREIGN KEY ([LivroId]) REFERENCES [Livros] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WishlistItems_Wishlists_WishlistId] FOREIGN KEY ([WishlistId]) REFERENCES [Wishlists] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615223129_AddWishlist'
)
BEGIN
    CREATE INDEX [IX_WishlistItems_LivroId] ON [WishlistItems] ([LivroId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615223129_AddWishlist'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WishlistItems_WishlistId_LivroId] ON [WishlistItems] ([WishlistId], [LivroId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615223129_AddWishlist'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Wishlists_ClienteId] ON [Wishlists] ([ClienteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615223129_AddWishlist'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260615223129_AddWishlist', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615230537_AddAvaliacoes'
)
BEGIN
    CREATE TABLE [Avaliacoes] (
        [Id] int NOT NULL IDENTITY,
        [ClienteId] int NOT NULL,
        [PedidoId] int NOT NULL,
        [LivroId] int NOT NULL,
        [Nota] int NOT NULL,
        [Comentario] nvarchar(1000) NULL,
        [DataAvaliacao] datetime2 NOT NULL,
        CONSTRAINT [PK_Avaliacoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Avaliacoes_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Avaliacoes_Livros_LivroId] FOREIGN KEY ([LivroId]) REFERENCES [Livros] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Avaliacoes_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615230537_AddAvaliacoes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Avaliacoes_ClienteId_PedidoId_LivroId] ON [Avaliacoes] ([ClienteId], [PedidoId], [LivroId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615230537_AddAvaliacoes'
)
BEGIN
    CREATE INDEX [IX_Avaliacoes_LivroId] ON [Avaliacoes] ([LivroId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615230537_AddAvaliacoes'
)
BEGIN
    CREATE INDEX [IX_Avaliacoes_PedidoId] ON [Avaliacoes] ([PedidoId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615230537_AddAvaliacoes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260615230537_AddAvaliacoes', N'8.0.25');
END;
GO

COMMIT;
GO

