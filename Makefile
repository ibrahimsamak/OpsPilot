.DEFAULT_GOAL := help
.PHONY: help dev infra down stop tokens clean logs

ORCH := src/OpsPilot.Orchestrator

help: ## Show this help
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) \
		| awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-10s\033[0m %s\n", $$1, $$2}'

dev: ## Start the full stack (infra + OpsApi + Orchestrator + Angular)
	@bash scripts/dev.sh

infra: ## Start only the Docker infra (Postgres + Aspire dashboard)
	@docker compose up -d

down: ## Stop the Docker infra (keeps the data volume)
	@docker compose down

stop: ## Stop just the app processes (leaves infra running)
	@pkill -f 'OpsPilot.OpsApi' 2>/dev/null || true
	@pkill -f 'OpsPilot.Orchestrator' 2>/dev/null || true
	@pkill -f 'ng serve' 2>/dev/null || true
	@echo "App processes stopped."

tokens: ## (Re)generate dev JWTs into web dev-tokens.ts
	@bash scripts/gen-tokens.sh

logs: ## Tail the combined app logs
	@tail -n +1 -F logs/*.log

clean: down ## Stop infra and remove logs + the Postgres data volume
	@docker compose down -v
	@rm -rf logs
	@echo "Cleaned infra volume and logs."
