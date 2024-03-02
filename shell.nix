{ pkgs ? import (builtins.fetchTarball {
  url =
    "https://github.com/NixOS/nixpkgs/archive/3da785eeaad3d604ee3bccc0a3f07bfd11cb355a.tar.gz";
  sha256 = "16vkl710mmq176qcj6ygk70kns37fg71ip6x16m6dhpicz90d2gq";
}) { } }:

let
  dependencies = with pkgs; [
    dotnetCorePackages.sdk_8_0
    glfw
    SDL2
    libGL
    openal
    glibc
    freetype
    fluidsynth
    soundfont-fluid
    gtk3
    pango
    cairo
    atk
    zlib
    glib
    gdk-pixbuf
    nss
    nspr
    at-spi2-atk
    libdrm
    expat
    libxkbcommon
    xorg.libxcb
    xorg.libX11
    xorg.libXcomposite
    xorg.libXdamage
    xorg.libXext
    xorg.libXfixes
    xorg.libXrandr
    xorg.libxshmfence
    mesa
    alsa-lib
    dbus
    at-spi2-core
    cups
  ];
in pkgs.mkShell {
  name = "space-station-14-devshell";
  buildInputs = [ pkgs.gtk3 ];
  packages = dependencies;
  shellHook = ''
    export GLIBC_TUNABLES=glibc.rtld.dynamic_sort=1
    export ROBUST_SOUNDFONT_OVERRIDE=${pkgs.soundfont-fluid}/share/soundfonts/FluidR3_GM2-2.sf2
    export XDG_DATA_DIRS=$GSETTINGS_SCHEMAS_PATH
    export LD_LIBRARY_PATH=${pkgs.lib.makeLibraryPath dependencies}
  '';
}
