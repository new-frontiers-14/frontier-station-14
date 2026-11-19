"""Optional discord.py extension. Run alongside service.py against the SAME SQLite file."""
import asyncio
import os
import time
from collections import defaultdict, deque

from discord import app_commands, Interaction, Object
from discord.ext import commands

from .service import Store


class SubscriptionLink(commands.Cog):
    def __init__(self):
        self.store = Store(os.environ["UNEON_DB"])
        self.guild_id = int(os.environ["DISCORD_GUILD_ID"])
        self.attempts = defaultdict(deque)

    @app_commands.command(name="uneon_link", description="Получить одноразовый код для привязки аккаунта SS14")
    @app_commands.guild_only()
    async def link(self, interaction: Interaction):
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
            code = await asyncio.to_thread(self.store.issue_discord, str(interaction.user.id))
        except Exception:
            await interaction.followup.send("Сервис временно недоступен. Попробуй позже.", ephemeral=True)
        else:
            await interaction.followup.send(
                "Вставь эту команду в консоль игры (F3), находясь на сервере UNEON:\n"
                f"```text\nuneon_subscription link {code}\n```\n"
                "Код действует 10 минут и используется один раз. Новый код отменяет предыдущий. "
                "Не передавай его другим людям. После привязки проверь подписку командой `uneon_subscription`.",
                ephemeral=True)


async def setup(bot: commands.Bot):
    cog = SubscriptionLink()
    await bot.add_cog(cog, guild=Object(id=cog.guild_id))
    # CommandTree.sync belongs to the HOST BOT, not this extension: do not overwrite its other commands.
