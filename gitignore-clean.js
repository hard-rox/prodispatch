#!/usr/bin/env node

const fs = require("fs");
const path = require("path");
const readline = require("readline");
const { execSync } = require("child_process");

const colors = {
  reset: "\x1b[0m",
  red: "\x1b[31m",
  green: "\x1b[32m",
  yellow: "\x1b[33m",
  blue: "\x1b[34m",
  cyan: "\x1b[36m",
  bright: "\x1b[1m",
};

const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout,
});

function ask(question) {
  return new Promise((resolve) => {
    rl.question(question, resolve);
  });
}

async function main() {
  console.clear();

  console.log(
    `${colors.cyan}${colors.bright}GitIgnore Cleanup Utility${colors.reset}\n`
  );

  const repoPath = await ask(
    `${colors.yellow}Enter repository path:${colors.reset} `
  );

  if (!fs.existsSync(repoPath)) {
    console.error(
      `${colors.red}Directory does not exist.${colors.reset}`
    );
    process.exit(1);
  }

  const gitignorePath = path.join(repoPath, ".gitignore");

  if (!fs.existsSync(gitignorePath)) {
    console.error(
      `${colors.red}.gitignore not found.${colors.reset}`
    );
    process.exit(1);
  }

  console.log(
    `\n${colors.blue}Scanning .gitignore matches...${colors.reset}`
  );

  let matches = [];

  try {
    const output = execSync(
      "git ls-files --others --ignored --exclude-standard",
      {
        cwd: repoPath,
        encoding: "utf8",
      }
    );

    matches = output
      .split(/\r?\n/)
      .filter(Boolean)
      .map((p) => path.join(repoPath, p));
  } catch (err) {
    console.error(
      `${colors.red}Failed to execute git command.${colors.reset}`
    );
    console.error(
      "Make sure the selected directory is a Git repository."
    );
    process.exit(1);
  }

  if (matches.length === 0) {
    console.log(
      `${colors.green}No ignored files found.${colors.reset}`
    );
    process.exit(0);
  }

  console.log(
    `\n${colors.bright}${colors.yellow}Items that will be deleted:${colors.reset}`
  );

  matches.forEach((item) => {
    const rel = path.relative(repoPath, item);

    if (
      fs.existsSync(item) &&
      fs.lstatSync(item).isDirectory()
    ) {
      console.log(`${colors.red}[DIR]  ${rel}${colors.reset}`);
    } else {
      console.log(`${colors.yellow}[FILE] ${rel}${colors.reset}`);
    }
  });

  console.log(
    `\n${colors.red}${colors.bright}Total: ${matches.length} item(s)${colors.reset}`
  );

  const answer = await ask(
    `\n${colors.cyan}Proceed with deletion? (yes/no): ${colors.reset}`
  );

  if (answer.trim().toLowerCase() !== "yes") {
    console.log(
      `${colors.yellow}Operation cancelled.${colors.reset}`
    );
    process.exit(0);
  }

  let deleted = 0;

  for (const item of matches) {
    try {
      if (fs.existsSync(item)) {
        fs.rmSync(item, {
          recursive: true,
          force: true,
        });

        console.log(
          `${colors.green}Deleted: ${path.relative(
            repoPath,
            item
          )}${colors.reset}`
        );

        deleted++;
      }
    } catch (err) {
      console.log(
        `${colors.red}Failed: ${path.relative(
          repoPath,
          item
        )}${colors.reset}`
      );
    }
  }

  console.log(
    `\n${colors.green}${colors.bright}Completed. Deleted ${deleted} item(s).${colors.reset}`
  );

  rl.close();
}

main();