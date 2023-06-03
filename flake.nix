{
  description = "Doge";

  nixConfig.bash-prompt-suffix = "dev$ ";

  inputs = {
    nixpkgs = {
      type = "github";
      owner = "NixOS";
      repo = "nixpkgs";
      ref = "nixos-22.11";
    };
  };

  outputs = { self, nixpkgs }:
    let pkgs = nixpkgs.legacyPackages.x86_64-linux; in {
      devShell.x86_64-linux = pkgs.mkShell {
        packages = [ pkgs.dotnet-sdk pkgs.csfml ];

        # RUSTFLAGS = "-A dead_code";
    };
  };
}
