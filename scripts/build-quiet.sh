#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"
mkdir -p .builds
log="$root/.builds/build.log"
exec 3>&1
exec >"$log" 2>&1
trap 'status=$?; if (( status != 0 )); then printf "Build or verification failed (exit %s). Log: %s\n" "$status" "$log" >&3; tail -25 "$log" >&3; fi' EXIT

expected=e9ed42e8664070678a68f3ba9266d26aeb4948098a87bb2aafaaa9287e53b577
verify_old() {
	python3 - "$expected" <<'PY'
import hashlib, sys
from pathlib import Path
path = Path('1.6/Assemblies/GUILocator.dll')
if hashlib.sha256(path.read_bytes()).hexdigest() != sys.argv[1]:
    raise SystemExit('The 1.6 DLL differs from the published release.')
PY
}
verify_old
for arg in "$@"; do
	if [[ "$(printf '%s' "$arg" | tr '[:upper:]' '[:lower:]')" == *rimworld_mod_dir* ]]; then
		printf 'Set RIMWORLD_MOD_DIR in the environment, not as an MSBuild argument.\n' >&2
		exit 2
	fi
done
if [[ -n "${RIMWORLD_MOD_DIR:-}" ]]; then
	python3 scripts/rimworld_deploy_guard.py check-stopped
fi
dotnet build Source/GUILocator.csproj -v:q -clp:ErrorsOnly "$@"
verify_old
if [[ -n "${RIMWORLD_MOD_DIR:-}" ]]; then
	python3 - "$RIMWORLD_MOD_DIR" <<'PY'
from pathlib import Path
from zipfile import ZipFile
import sys
root = Path(sys.argv[1])
installed = root / 'GUILocator'
for name in ('1.6/Assemblies/GUILocator.dll', '1.7/Assemblies/GUILocator.dll'):
    if Path(name).read_bytes() != (installed / name).read_bytes():
        raise SystemExit(f'Deployed file differs: {name}')
files = {str(path.relative_to(installed)): path.read_bytes() for path in installed.rglob('*') if path.is_file()}
with ZipFile(root / 'GUILocator.zip') as archive:
    members = {name: archive.read(name) for name in archive.namelist() if not name.endswith('/')}
if files != members:
    raise SystemExit('ZIP does not match deployed files.')
PY
fi
printf 'ok\n' >&3
