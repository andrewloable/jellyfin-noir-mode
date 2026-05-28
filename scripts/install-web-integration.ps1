param(
    [string] $Container = 'jellyfin',
    [string] $IndexPath = '/usr/share/jellyfin/web/index.html'
)

$ErrorActionPreference = 'Stop'

$containerScript = @'
set -eu

index="__INDEX_PATH__"
marker_start='<!-- Jellyfin.Plugin.NoirMode web integration start -->'
marker_end='<!-- Jellyfin.Plugin.NoirMode web integration end -->'
script_tag='<script defer src="../NoirMode/web/video-page.js"></script>'

if [ ! -f "$index" ]; then
    echo "Jellyfin Web index.html was not found at $index" >&2
    exit 1
fi

if ! command -v perl >/dev/null 2>&1; then
    echo "perl is required inside the Jellyfin container to patch index.html" >&2
    exit 2
fi

backup="/config/data/plugins/configurations/jellyfin-web-index.noir-backup.$(date +%Y%m%d%H%M%S).html"
cp "$index" "$backup"

perl -0pi -e '
    s/<!-- Jellyfin\.Plugin\.NoirMode web integration start -->.*?<!-- Jellyfin\.Plugin\.NoirMode web integration end -->\R?//s;
    my $block = "<!-- Jellyfin.Plugin.NoirMode web integration start -->\n<script defer src=\"../NoirMode/web/video-page.js\"></script>\n<!-- Jellyfin.Plugin.NoirMode web integration end -->\n";
    if (!s#</body>#$block</body>#i) {
        $_ .= "\n$block";
    }
' "$index"

grep -q "$script_tag" "$index"
echo "Noir Mode web integration installed in $index"
echo "Backup written to $backup"
'@

$containerScript = $containerScript.Replace('__INDEX_PATH__', $IndexPath.Replace('"', '\"'))
$containerScript | docker exec -i $Container sh
