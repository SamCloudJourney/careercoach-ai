# .NET Bounty Lab - see README.md
SHELL := /bin/bash
TARGET ?= json-document
TFM ?= net11.0
FUZZ_SECONDS ?= 120

.PHONY: all scope sdks source map surface build matrix fuzz libfuzzer diff templates clean-findings

all: scope sdks source map surface matrix fuzz diff templates

scope:      ; ./scripts/00-check-scope.sh
sdks:       ; ./scripts/01-install-sdks.sh
source:     ; ./scripts/02-fetch-source.sh
map:        ; ./scripts/03-link-components.sh
surface:    ; ./scripts/04-attack-surface.sh
matrix:     ; ./scripts/05-run-matrix.sh
fuzz:       ; ./scripts/06-fuzz.sh $(FUZZ_SECONDS)
diff:       ; ./scripts/08-patch-diff.sh
templates:  ; ./scripts/09-template-audit.sh

# Opt-in: downloads + compiles third-party fuzzing tooling. Review scripts/07-libfuzzer.sh first.
libfuzzer:  ; ./scripts/07-libfuzzer.sh $(TARGET) $(TFM) $(FUZZ_SECONDS)

build:
	source scripts/env.sh >/dev/null && dotnet build labs/BountyLab.slnx -c Release

clean-findings:
	rm -rf labs/findings notes/logs
