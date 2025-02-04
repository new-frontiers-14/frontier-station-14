import logging

from yaml import SafeLoader
from typing import List, Union
from logging import Logger, getLogger

from config import ILLEGAL_MATCHES, LEGAL_OVERRIDES, CONDITIONALLY_ILLEGAL_MATCHES


def get_logger(debug: bool = False) -> Logger:
    """
    Gets a logger for use by MapChecker.

    :return: A logger.
    """
    logger = getLogger("MapChecker")
    logger.setLevel("DEBUG" if debug else "INFO")

    sh = logging.StreamHandler()
    formatter = logging.Formatter(
        "[%(asctime)s %(levelname)7s] %(message)s",
        datefmt='%Y-%m-%d %H:%M:%S'
    )
    sh.setFormatter(formatter)
    logger.addHandler(sh)

    return logger


# Snippet taken from https://stackoverflow.com/questions/33048540/pyyaml-safe-load-how-to-ignore-local-tags
class YamlLoaderIgnoringTags(SafeLoader):
    def ignore_unknown(self, node):
        return None


YamlLoaderIgnoringTags.add_constructor(None, YamlLoaderIgnoringTags.ignore_unknown)
# End of snippet


def check_prototype(proto_id: str, proto_name: str, proto_suffixes: List[str]) -> Union[bool, List[str]]:
    """
    Checks prototype information against the ILLEGAL_MATCHES and CONDITIONALLY_ILLEGAL_MATCHES constants.

    :param proto_id: The prototype's ID.
    :param proto_name: The prototype's name.
    :param proto_suffixes: The prototype's suffixes.
    :return:
    - True if the prototype is legal
    - False if the prototype is globally illegal (matched by ILLEGAL_MATCHES)
    - A list of shipyard keys if the prototype is conditionally illegal (matched by CONDITIONALLY_ILLEGAL_MATCHES)
    """
    # Check against LEGAL_OVERRIDES (no suffix!)
    for legal_match in LEGAL_OVERRIDES:
        if legal_match.lower() in proto_name.lower():
            return True

        if legal_match.lower() in proto_id.lower():
            return True

    # Check against ILLEGAL_MATCHES.
    for illegal_match in ILLEGAL_MATCHES:
        if illegal_match.lower() in proto_name.lower():
            return False

        if illegal_match.lower() in proto_id.lower():
            return False

        for suffix in proto_suffixes:
            if illegal_match.lower() == suffix.lower():
                return False

    # Check against CONDITIONALLY_ILLEGAL_MATCHES.
    conditionally_illegal_keys = list()
    for key in CONDITIONALLY_ILLEGAL_MATCHES.keys():

        cond_illegal_matches = CONDITIONALLY_ILLEGAL_MATCHES[key]
        for cond_illegal_match in cond_illegal_matches:

            if cond_illegal_match.lower() in proto_name.lower():
                conditionally_illegal_keys.append(key)
                break

            if cond_illegal_match.lower() in proto_id.lower():
                conditionally_illegal_keys.append(key)
                break

            for suffix in proto_suffixes:
                if cond_illegal_match.lower() == suffix.lower():
                    conditionally_illegal_keys.append(key)
                    break

    if len(conditionally_illegal_keys) > 0:
        return conditionally_illegal_keys

    return True
