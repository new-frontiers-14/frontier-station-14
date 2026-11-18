"""Optional discord.py extension. Run alongside service.py against the SAME SQLite file."""
import asyncio
import os
import time
from collections import defaultdict, deque

from discord import app_commands, Interaction, Object
from discord.ext import commands

from .service import Rejected, Store


class SubscriptionLink(commands.Cog):
    def __init__(self):
        self.store = Store(os.environ["UNEON_DB"])
        self.guild_id = int(os.environ["DISCORD_GUILD_ID"])
        self.attempts = defaultdict(deque)

    @app_commands.command(name="uneon_link", description="Привязать аккаунт SS14 по коду из игры")
    @app_commands.guild_only()
    async def link(self, interaction: Interaction, code: str):
        if interaction.guild_id != self.guild_id:
            await interaction.response.send_message("Команда доступна только на сервере UNEON.", ephemeral=True)
            return
        now = time.monotonic()
        attempts = self.attempts[interaction.user.id]
        while attempts and attempts[0] < now - 60:
            attempts.popleft()
        if len(attempts) >= 5:
            await interaction.response.send_message("Подожди минуту перед следующей попыткой.", ephemeral=True)
            return
        attempts.append(now)
        await interaction.response.defer(ephemeral=True)
        try:
            await asyncio.to_thread(self.store.link, code, str(interaction.user.id))
        except Rejected:
            await interaction.followup.send("Код неверен, истёк или аккаунт уже привязан. Получи новый код в игре; для смены привязки сначала отвяжи аккаунт в игре.", ephemeral=True)
        except Exception:
            await interaction.followup.send("Сервис временно недоступен. Попробуй позже.", ephemeral=True)
        else:
            await interaction.followup.send("Аккаунт привязан. Проверь подписку в игре командой uneon_subscription.", ephemeral=True)


async def setup(bot: commands.Bot):
    cog = SubscriptionLink()
    await bot.add_cog(cog, guild=Object(id=cog.guild_id))
    # CommandTree.sync belongs to the HOST BOT, not this extension: do not overwrite its other commands.
