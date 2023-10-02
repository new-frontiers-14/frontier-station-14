@echo off
copy runconfigdev.toml server_config.toml
move server_config.toml ../../bin/Content.Server/
