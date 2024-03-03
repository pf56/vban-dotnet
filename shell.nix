{ pkgs ? import <nixpkgs> {} }:

  pkgs.mkShell {
    nativeBuildInputs = with pkgs; [ dotnet-sdk_8 dotnet-runtime_8 libjack2 libpulseaudio ];
  }
