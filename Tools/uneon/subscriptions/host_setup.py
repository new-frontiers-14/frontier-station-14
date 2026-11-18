"""Opt-in startup hook for the installed monitor; never replaces its polling logic."""
import asyncio
import logging
import os

import discord


async def enable_subscription_link(bot):
    if os.environ.get("UNEON_SUBSCRIPTIONS_ENABLED") != "1":
        return
    log = logging.getLogger("ss14_monitor.subscriptions")
    extension = "uneon_subscriptions.discord_extension"
    loaded = False
    try:
        guild = discord.Object(id=int(os.environ["DISCORD_GUILD_ID"]))
        async with asyncio.timeout(30):
            await bot.load_extension(extension)
            loaded = True
            local = {(c.name, getattr(c, "type", discord.AppCommandType.chat_input))
                     for c in bot.tree.get_commands(guild=guild)}
            remote = {(c.name, c.type) for c in await bot.tree.fetch_commands(guild=guild)}
            if remote - local:
                raise RuntimeError("Refusing sync: existing remote commands are missing locally")
            await bot.tree.sync(guild=guild)
        log.info("UNEON account linking enabled for configured guild")
    except Exception as exc:
        if loaded:
            try:
                await bot.unload_extension(extension)
            except Exception:
                log.error("Could not unload account linking extension")
        # No remote exception payloads or tokens in logs. Monitoring still starts.
        log.error("Account linking unavailable (%s); monitoring remains enabled", type(exc).__name__)
