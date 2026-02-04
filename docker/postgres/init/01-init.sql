-- Script de inicialização do banco ExamAI
-- Executado automaticamente na primeira criação do container

-- Criar extensões úteis
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";  -- Para buscas fuzzy

-- Mensagem de sucesso
DO $$
BEGIN
    RAISE NOTICE '🎉 ExamAI Database initialized successfully!';
    RAISE NOTICE '📊 Database: examai';
    RAISE NOTICE '👤 User: postgres';
    RAISE NOTICE '🔌 Extensions: uuid-ossp, pg_trgm';
END
$$;
