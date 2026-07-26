#!/usr/bin/bash
mono "$(dirname "$0")/../../shared/tools/RTGen/bin/rtgen.exe" --language=c --inputLanguage=cbatch --namespace=daq --source="$(dirname "$0")/../../core" --outputDir="$(dirname "$0")"
