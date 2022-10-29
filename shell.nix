{ pkgs ? import <nixpkgs> {} }:

  pkgs.mkShell {
    nativeBuildInputs = with pkgs; [ jetbrains.rider dotnet-sdk dotnet-runtime libjack2 libpulseaudio ];
  }
