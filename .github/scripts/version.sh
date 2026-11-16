#!/usr/bin/env bash
# PR 제목으로 버전 증가 종류를 정하고, 다음 버전을 계산한다.
#
#   version.sh bump  "<PR 제목>"                 → major | minor | patch   (형식이 틀리면 종료 코드 1)
#   version.sh next  "<PR 제목>" <현재 버전>      → 다음 버전 (예: 1.4.2)
#
# PR 제목 규칙 (README "PR 제목 규칙" 참고):
#   <종류>[(범위)][!]: <설명>
#   - 느낌표(!)가 있거나 종류가 major      → major
#   - feat                                 → minor
#   - fix perf refactor data docs chore ci build test style → patch
set -euo pipefail

TYPES_MINOR='feat'
TYPES_PATCH='fix|perf|refactor|data|docs|chore|ci|build|test|style'
PATTERN="^(major|${TYPES_MINOR}|${TYPES_PATCH})(\([^()]+\))?(!)?: .+"

bump_of() {
  local title="$1"
  if [[ ! "$title" =~ $PATTERN ]]; then
    return 1
  fi
  local type="${BASH_REMATCH[1]}" bang="${BASH_REMATCH[3]}"
  if [[ -n "$bang" || "$type" == "major" ]]; then
    echo major
  elif [[ "$type" =~ ^(${TYPES_MINOR})$ ]]; then
    echo minor
  else
    echo patch
  fi
}

next_of() {
  local bump="$1" current="${2#v}"
  if [[ ! "$current" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)$ ]]; then
    echo "현재 버전 형식이 잘못되었습니다: '$2'" >&2
    return 1
  fi
  local major="${BASH_REMATCH[1]}" minor="${BASH_REMATCH[2]}" patch="${BASH_REMATCH[3]}"
  case "$bump" in
    major) echo "$((major + 1)).0.0" ;;
    minor) echo "${major}.$((minor + 1)).0" ;;
    patch) echo "${major}.${minor}.$((patch + 1))" ;;
  esac
}

usage_error() {
  cat >&2 <<'EOF'
PR 제목이 규칙에 맞지 않습니다. 형식: <종류>[(범위)][!]: <설명>
  major / 느낌표(!)  → 메이저 버전   예) feat!: 설정 파일 형식 변경
  feat               → 마이너 버전   예) feat: 다음 단계 단축키 추가
  fix perf refactor data docs chore ci build test style → 패치 버전
                                     예) data(3장): 도서관 단계 위치 수정
EOF
}

case "${1:-}" in
  bump)
    bump_of "${2:-}" || { usage_error; exit 1; }
    ;;
  next)
    bump="$(bump_of "${2:-}")" || { usage_error; exit 1; }
    next_of "$bump" "${3:-0.0.0}"
    ;;
  *)
    echo "사용법: $0 bump <PR 제목> | next <PR 제목> <현재 버전>" >&2
    exit 2
    ;;
esac
