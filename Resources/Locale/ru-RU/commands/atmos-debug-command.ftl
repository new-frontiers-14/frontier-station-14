cmd-atvrange-desc = Задает диапазон отладки atmos (в виде двух плавающих значений, начало [red] и конец [blue])
cmd-atvrange-help = Использование: { $command } <start> <end>
cmd-atvrange-error-start = Неудачный СТАРТ
cmd-atvrange-error-end = Неудачный КОНЕЦ
cmd-atvrange-error-zero = Масштаб не может быть нулевым, так как это приведет к делению на ноль в AtmosDebugOverlay.

cmd-atvmode-desc = Устанавливает режим отладки atmos. При этом автоматически сбрасывается шкала.
cmd-atvmode-help = Использование: { $command } <TotalMoles/GasMoles/Temperature> [<gas ID (for GasMoles)>]
cmd-atvmode-error-invalid = Недопустимый режим
cmd-atvmode-error-target-gas = Для этого режима необходимо обеспечить целевой газ.
cmd-atvmode-error-out-of-range = Идентификатор газа не может быть разобран или находится вне диапазона.
cmd-atvmode-error-info = Для этого режима не требуется никакой дополнительной информации.

cmd-atvcbm-desc = Переход от красного/зеленого/синего к оттенкам серого
cmd-atvcbm-help = Использование: { $command } <true/false>
cmd-atvcbm-error = Недопустимый флаг
